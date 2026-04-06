using Sklad_Kursach.Class;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace Sklad_Kursach.Pages
{
    public partial class Users_Page : Page
    {
        public class UserRow
        {
            public int EmployeeId { get; set; }
            public int AuthId { get; set; }
            public string FullName { get; set; }
            public string Role { get; set; }
            public string Login { get; set; }
            public string Password { get; set; }
            public string LastLogin { get; set; }
            public int IncomingToday { get; set; }
            public int SortToday { get; set; }
        }

        public Users_Page()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!UserData.EnsureAuthorized(this))
                    return;

                if ((UserData.CurrentUser.Role ?? "") == "Администратор")
                {
                    AdminUserPanel.Visibility = Visibility.Visible;
                    PasswordColumn.Visibility = Visibility.Visible;
                }
                else
                {
                    AdminUserPanel.Visibility = Visibility.Collapsed;
                    PasswordColumn.Visibility = Visibility.Collapsed;
                }

                LoadUsers();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка загрузки страницы сотрудников:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void LoadUsers()
        {
            try
            {
                string connStr = ConfigurationManager.ConnectionStrings["Warehouse_DB_V3"]?.ConnectionString;
                if (string.IsNullOrWhiteSpace(connStr))
                    throw new Exception("Строка подключения к БД не найдена.");

                List<UserRow> list = new List<UserRow>();

                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    string query = @"
                        SELECT 
                            E.Employee_id,
                            E.Auth_id,
                            (F.Last_name + ' ' + F.First_name + ' ' + ISNULL(F.Middle_name, '')) AS FullName,
                            P.Post_Name,
                            A.Login,
                            A.Password,
                            A.LastVhod,
                            (
                                SELECT COUNT(*) 
                                FROM ActionLog AL 
                                WHERE AL.Employee_id = E.Employee_id 
                                  AND AL.ActionType = 'INCOMING' 
                                  AND CAST(AL.ActionTime AS DATE) = CAST(GETDATE() AS DATE)
                            ) AS IncToday,
                            (
                                SELECT COUNT(*) 
                                FROM ActionLog AL 
                                WHERE AL.Employee_id = E.Employee_id 
                                  AND AL.ActionType = 'SORT' 
                                  AND CAST(AL.ActionTime AS DATE) = CAST(GETDATE() AS DATE)
                            ) AS SortToday
                        FROM Employee E
                        JOIN FIO F ON E.FIO_id = F.FIO_id
                        JOIN Post P ON E.Post_id = P.Post_id
                        JOIN Data_for_authorization A ON E.Auth_id = A.Auth_id";

                    SqlCommand cmd = new SqlCommand(query, conn);

                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            list.Add(new UserRow
                            {
                                EmployeeId = Convert.ToInt32(r["Employee_id"]),
                                AuthId = Convert.ToInt32(r["Auth_id"]),
                                FullName = r["FullName"]?.ToString(),
                                Role = r["Post_Name"]?.ToString(),
                                Login = r["Login"]?.ToString(),
                                Password = r["Password"]?.ToString(),
                                LastLogin = r["LastVhod"] != DBNull.Value ? r["LastVhod"].ToString() : "—",
                                IncomingToday = Convert.ToInt32(r["IncToday"]),
                                SortToday = Convert.ToInt32(r["SortToday"])
                            });
                        }
                    }
                }

                UsersGrid.ItemsSource = list;
            }
            catch (SqlException ex)
            {
                MessageBox.Show(
                    "Ошибка загрузки сотрудников из базы данных:\n" + ex.Message,
                    "SQL ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось загрузить сотрудников:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void AddUser_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AddUserOverlay.Visibility = Visibility.Visible;
                NewSurnameTb.Focus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка открытия формы добавления:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CancelAdd_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AddUserOverlay.Visibility = Visibility.Collapsed;
                NewSurnameTb.Clear();
                NewNameTb.Clear();
                NewPatronymicTb.Clear();
                NewLoginTb.Clear();
                NewPassTb.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка закрытия формы:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void SaveUser_Click(object sender, RoutedEventArgs e)
        {
            if (!UserData.EnsureAuthorized(this))
                return;

            try
            {
                if (string.IsNullOrWhiteSpace(NewSurnameTb.Text) ||
                    string.IsNullOrWhiteSpace(NewNameTb.Text) ||
                    string.IsNullOrWhiteSpace(NewLoginTb.Text) ||
                    string.IsNullOrWhiteSpace(NewPassTb.Text))
                {
                    MessageBox.Show(
                        "Заполните все обязательные поля!",
                        "Предупреждение",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                int postId = 3;
                if (NewRoleCb.SelectedIndex == 0) postId = 1;
                else if (NewRoleCb.SelectedIndex == 1) postId = 2;

                string connStr = ConfigurationManager.ConnectionStrings["Warehouse_DB_V3"]?.ConnectionString;
                if (string.IsNullOrWhiteSpace(connStr))
                    throw new Exception("Строка подключения к БД не найдена.");

                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    SqlTransaction transaction = conn.BeginTransaction();

                    try
                    {
                        SqlCommand cmdFio = new SqlCommand(
                            @"INSERT INTO FIO (Last_name, First_name, Middle_name) 
                              VALUES (@fam, @im, @otch); 
                              SELECT SCOPE_IDENTITY();",
                            conn, transaction);

                        cmdFio.Parameters.AddWithValue("@fam", NewSurnameTb.Text.Trim());
                        cmdFio.Parameters.AddWithValue("@im", NewNameTb.Text.Trim());
                        cmdFio.Parameters.AddWithValue("@otch",
                            string.IsNullOrWhiteSpace(NewPatronymicTb.Text)
                                ? (object)DBNull.Value
                                : NewPatronymicTb.Text.Trim());

                        int fioId = Convert.ToInt32(cmdFio.ExecuteScalar());

                        SqlCommand cmdAuth = new SqlCommand(
                            @"INSERT INTO Data_for_authorization (Login, Password) 
                              VALUES (@log, @pass); 
                              SELECT SCOPE_IDENTITY();",
                            conn, transaction);

                        cmdAuth.Parameters.AddWithValue("@log", NewLoginTb.Text.Trim());
                        cmdAuth.Parameters.AddWithValue("@pass", NewPassTb.Text.Trim());

                        int authId = Convert.ToInt32(cmdAuth.ExecuteScalar());

                        SqlCommand cmdEmp = new SqlCommand(
                            "INSERT INTO Employee (Post_id, FIO_id, Auth_id) VALUES (@postId, @fioId, @authId)",
                            conn, transaction);

                        cmdEmp.Parameters.AddWithValue("@postId", postId);
                        cmdEmp.Parameters.AddWithValue("@fioId", fioId);
                        cmdEmp.Parameters.AddWithValue("@authId", authId);
                        cmdEmp.ExecuteNonQuery();

                        transaction.Commit();
                    }
                    catch
                    {
                        try { transaction.Rollback(); } catch { }
                        throw;
                    }
                }

                MessageBox.Show(
                    "Сотрудник добавлен!",
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                CancelAdd_Click(null, null);
                LoadUsers();
            }
            catch (SqlException ex)
            {
                MessageBox.Show(
                    "Ошибка базы данных при добавлении сотрудника:\n" + ex.Message,
                    "SQL ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка при добавлении сотрудника:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void DeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (!UserData.EnsureAuthorized(this))
                return;

            try
            {
                UserRow user = UsersGrid.SelectedItem as UserRow;
                if (user == null)
                {
                    MessageBox.Show(
                        "Выберите сотрудника.",
                        "Предупреждение",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                if (user.AuthId == UserData.CurrentUser.AuthId)
                {
                    MessageBox.Show(
                        "Нельзя удалить себя.",
                        "Предупреждение",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                if (MessageBox.Show(
                    $"Уволить {user.FullName}?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) != MessageBoxResult.Yes)
                {
                    return;
                }

                string connStr = ConfigurationManager.ConnectionStrings["Warehouse_DB_V3"]?.ConnectionString;
                if (string.IsNullOrWhiteSpace(connStr))
                    throw new Exception("Строка подключения к БД не найдена.");

                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    SqlCommand cmd = new SqlCommand(
                        "UPDATE Data_for_authorization SET Login = @log, Password = '---' WHERE Auth_id = @id",
                        conn);

                    cmd.Parameters.AddWithValue("@log", "FIRED_" + user.Login + "_" + new Random().Next(100));
                    cmd.Parameters.AddWithValue("@id", user.AuthId);
                    cmd.ExecuteNonQuery();

                    SqlCommand cmdName = new SqlCommand(
                        "UPDATE FIO SET Last_name = Last_name + ' (УВОЛЕН)' WHERE FIO_id = (SELECT FIO_id FROM Employee WHERE Employee_id = @eid)",
                        conn);

                    cmdName.Parameters.AddWithValue("@eid", user.EmployeeId);
                    cmdName.ExecuteNonQuery();
                }

                LoadUsers();
            }
            catch (SqlException ex)
            {
                MessageBox.Show(
                    "Ошибка базы данных при увольнении сотрудника:\n" + ex.Message,
                    "SQL ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка при увольнении сотрудника:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void GoBack_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if ((UserData.CurrentUser.Role ?? "") == "Администратор")
                    NavigationService?.Navigate(new AdminHubPage());
                else
                    NavigationService?.Navigate(new UserHubPage());
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка возврата назад:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}