using Microsoft.Win32;
using Sklad_Kursach.Class;
using System;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Sklad_Kursach.Pages
{
    public partial class Profile_Page : Page
    {
        public Profile_Page()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!UserData.EnsureAuthorized(this))
                    return;

                UserName.Text = $"{UserData.CurrentUser.LastName} {UserData.CurrentUser.FirstName}";
                PostTb.Text = UserData.CurrentUser.Role;
                LoginTb.Text = UserData.CurrentUser.Login;
                LastVhod.Text = UserData.CurrentUser.LastLogin;

                UserData.LoadAvatar(UserData.CurrentUser.AuthId, null, AvatarEmoji, UserAvatar);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка загрузки профиля:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ChangePhoto_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!UserData.EnsureAuthorized(this))
                    return;

                OpenFileDialog op = new OpenFileDialog
                {
                    Title = "Выберите фото",
                    Filter = "Картинки|*.jpg;*.jpeg;*.png"
                };

                if (op.ShowDialog() != true)
                    return;

                if (!File.Exists(op.FileName))
                {
                    MessageBox.Show(
                        "Файл не найден.",
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                FileInfo fi = new FileInfo(op.FileName);
                if (fi.Length > 5 * 1024 * 1024)
                {
                    MessageBox.Show(
                        "Файл слишком большой. Выберите изображение до 5 МБ.",
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                byte[] imageBytes = File.ReadAllBytes(op.FileName);

                using (SqlConnection conn = new SqlConnection(UserData.GetConnectionString()))
                {
                    conn.Open();

                    SqlCommand cmd = new SqlCommand(
                        "UPDATE Employee SET Photo = @photo WHERE Auth_id = @id",
                        conn);

                    cmd.Parameters.AddWithValue("@photo", imageBytes);
                    cmd.Parameters.AddWithValue("@id", UserData.CurrentUser.AuthId);
                    cmd.ExecuteNonQuery();
                }

                UserData.LoadAvatar(UserData.CurrentUser.AuthId, null, AvatarEmoji, UserAvatar);

                MessageBox.Show(
                    "Фото успешно обновлено.",
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (IOException ex)
            {
                MessageBox.Show(
                    "Ошибка чтения файла:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (SqlException ex)
            {
                MessageBox.Show(
                    "Ошибка сохранения фото в БД:\n" + ex.Message,
                    "SQL ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось обновить фото:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void GoBack(object sender, RoutedEventArgs e)
        {
            try
            {
                if (NavigationService?.CanGoBack == true)
                    NavigationService.GoBack();
                else
                    NavigationService?.Navigate(new Auth_Page());
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