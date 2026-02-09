using Sklad_Kursach.Class;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Sklad_Kursach.Pages
{
    /// <summary>
    /// Логика взаимодействия для Auth_Page.xaml
    /// </summary>

    public partial class Auth_Page : Page
    {
        public Auth_Page()
        {
            InitializeComponent();

        }
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            UserData userFinder = new UserData();
            userFinder.GetUser(UsernameTB.Text, PasswordBoxe.Password);

            if (UserData.CurrentUser != null)
            {

                if (UserData.CurrentUser.Role == "Рабочий")
                {
                    NavigationService.Navigate(new UserHubPage());
                }
                else if (UserData.CurrentUser.Role == "Старший Рабочий" || UserData.CurrentUser.Role == "Администратор")
                {
                    NavigationService.Navigate(new AdminHubPage());

                }
            }
            else if (UserData.CurrentUser == null)
            {
                MessageBox.Show("Пользователь не найден :<", "Неудача", MessageBoxButton.OK, MessageBoxImage.Information);

            }
            else
            {
                MessageBox.Show("Неверный логин или пароль. Пожалуйста, попробуйте снова.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowPassword_PreviewMouseDown(object sender, MouseButtonEventArgs e) // показ пароля
        {

            TextBoxe.Text = PasswordBoxe.Password;

            TextBoxe.Visibility = Visibility.Visible;
            PasswordBoxe.Visibility = Visibility.Collapsed;

            TextBoxe.Focus();

            e.Handled = true;
        }

        private void HidePassword_PreviewMouseUp(object sender, MouseButtonEventArgs e) //снова прячем пароль
        {

            PasswordBoxe.Password = TextBoxe.Text;


            TextBoxe.Visibility = Visibility.Collapsed;
            PasswordBoxe.Visibility = Visibility.Visible;

            PasswordBoxe.Focus();

            e.Handled = true;
        }
    }
}
