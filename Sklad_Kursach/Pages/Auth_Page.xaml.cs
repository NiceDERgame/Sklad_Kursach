using Sklad_Kursach.Class;
using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Sklad_Kursach.Pages
{
    public partial class Auth_Page : Page
    {
        public Auth_Page()
        {
            InitializeComponent();
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(UsernameTB.Text) || string.IsNullOrWhiteSpace(PasswordBoxe.Password))
                {
                    MessageBox.Show(
                        "Введите логин и пароль.",
                        "Предупреждение",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                UserData userFinder = new UserData();
                userFinder.GetUser(UsernameTB.Text.Trim(), PasswordBoxe.Password);

                if (UserData.CurrentUser == null)
                {
                    MessageBox.Show(
                        "Неверный логин или пароль. Пожалуйста, попробуйте снова.",
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                string role = (UserData.CurrentUser.Role ?? string.Empty).ToLowerInvariant();

                if (role == "рабочий")
                {
                    NavigationService?.Navigate(new UserHubPage());
                }
                else if (role == "старший рабочий" || role == "администратор")
                {
                    NavigationService?.Navigate(new AdminHubPage());
                }
                else
                {
                    MessageBox.Show(
                        $"Роль '{UserData.CurrentUser.Role}' не распознана системой!",
                        "Ошибка доступа",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(
                    "Ошибка подключения к базе данных:\n" + ex.Message,
                    "SQL ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось выполнить вход:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ShowPassword_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            TextBoxe.Text = PasswordBoxe.Password;
            TextBoxe.Visibility = Visibility.Visible;
            PasswordBoxe.Visibility = Visibility.Collapsed;
            TextBoxe.Focus();
            e.Handled = true;
        }

        private void HidePassword_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            PasswordBoxe.Password = TextBoxe.Text;
            TextBoxe.Visibility = Visibility.Collapsed;
            PasswordBoxe.Visibility = Visibility.Visible;
            PasswordBoxe.Focus();
            e.Handled = true;
        }
    }
}