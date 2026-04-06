using Sklad_Kursach.Class;
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Sklad_Kursach.Pages
{
    public partial class AdminHubPage : Page
    {
        public AdminHubPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!UserData.EnsureAuthorized(this))
                    return;

                LoadStats();
                UserData.LoadAvatar(UserData.CurrentUser.AuthId, AvatarBorder, AvatarEmoji);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка загрузки страницы администратора:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void LoadStats()
        {
            string connStr = ConfigurationManager.ConnectionStrings["Warehouse_DB_V3"]?.ConnectionString;
            if (string.IsNullOrWhiteSpace(connStr))
                throw new Exception("Строка подключения к БД не найдена.");

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();

                // Всего позиций на складе
                SqlCommand cmdTotal = new SqlCommand(
                    "SELECT ISNULL(SUM(Quantity), 0) FROM LotPlacement",
                    conn);
                TotalItemsTb.Text = Convert.ToString(cmdTotal.ExecuteScalar());

                // Принято сегодня ИМЕННО текущим админом
                SqlCommand cmdMyIncomingToday = new SqlCommand(@"
                    SELECT COUNT(*) 
                    FROM ActionLog 
                    WHERE ActionType = 'INCOMING'
                      AND Employee_id = @empId
                      AND CAST(ActionTime AS DATE) = CAST(GETDATE() AS DATE)", conn);
                cmdMyIncomingToday.Parameters.AddWithValue("@empId", UserData.CurrentUser.EmployeeId);
                NewItemsTb.Text = Convert.ToString(cmdMyIncomingToday.ExecuteScalar());

                // Принято всего
                SqlCommand cmdIncomingTotal = new SqlCommand(@"
                    SELECT COUNT(*)
                    FROM ActionLog
                    WHERE ActionType = 'INCOMING'", conn);
                IncomingTotalTb.Text = Convert.ToString(cmdIncomingTotal.ExecuteScalar());

                // Истекает срок
                string sqlUrgent = @"
                    SELECT COUNT(*) 
                    FROM Lot l
                    LEFT JOIN LotPlacement lp ON l.Lot_id = lp.Lot_id
                    WHERE DATEADD(hour, l.ShelfLifeHours, CAST(l.ArrivalDate AS DATETIME)) < DATEADD(day, 3, GETDATE())";

                SqlCommand cmdUrgent = new SqlCommand(sqlUrgent, conn);
                UrgentItemsTb.Text = Convert.ToString(cmdUrgent.ExecuteScalar());

                // Отсортировано сегодня ИМЕННО текущим админом
                SqlCommand cmdMySortToday = new SqlCommand(@"
                    SELECT COUNT(*) 
                    FROM ActionLog 
                    WHERE ActionType = 'SORT'
                      AND Employee_id = @empId
                      AND CAST(ActionTime AS DATE) = CAST(GETDATE() AS DATE)", conn);
                cmdMySortToday.Parameters.AddWithValue("@empId", UserData.CurrentUser.EmployeeId);
                SortedItemsTb.Text = Convert.ToString(cmdMySortToday.ExecuteScalar());

                // Отсортировано всего
                SqlCommand cmdSortTotal = new SqlCommand(@"
                    SELECT COUNT(*)
                    FROM ActionLog
                    WHERE ActionType = 'SORT'", conn);
                SortedTotalTb.Text = Convert.ToString(cmdSortTotal.ExecuteScalar());
            }
        }

        private void GoProfile(object sender, RoutedEventArgs e) => NavigationService?.Navigate(new Profile_Page());
        private void GoInventory(object sender, RoutedEventArgs e) => NavigationService?.Navigate(new Inventory_Page());
        private void GoLogs(object sender, RoutedEventArgs e) => NavigationService?.Navigate(new Logs_Page());
        private void GoIncoming(object sender, RoutedEventArgs e) => NavigationService?.Navigate(new Incoming_Page());
        private void GoOutgoing(object sender, RoutedEventArgs e) { }
        private void GoWorkers(object sender, RoutedEventArgs e) => NavigationService?.Navigate(new Users_Page());

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            UserData.CurrentUser = null;
            NavigationService?.Navigate(new Auth_Page());
        }
    }
}