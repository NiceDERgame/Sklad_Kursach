using Sklad_Kursach.Class;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Sklad_Kursach.Pages
{
    public partial class Users_Page : Page
    {
        public Users_Page()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (UserData.CurrentUser.Role == "Администратор")
            {
                AdminUserPanel.Visibility = Visibility.Visible;
            }
        }

        // --- ЛОГИКА ВОЗВРАТА ---
        private void GoBack_Click(object sender, RoutedEventArgs e)
        {
            if (UserData.CurrentUser.Role == "Администратор" || UserData.CurrentUser.Role == "Старший Рабочий")
            {
                NavigationService.Navigate(new AdminHubPage());
            }
            else
            {
                NavigationService.Navigate(new UserHubPage());
            }
        }

        private void AddUser_Click(object sender, RoutedEventArgs e) { }
        private void EditUser_Click(object sender, RoutedEventArgs e) { }
        private void DeleteUser_Click(object sender, RoutedEventArgs e) { }
    }
}