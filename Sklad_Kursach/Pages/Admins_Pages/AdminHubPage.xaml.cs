using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Sklad_Kursach.Class; // Для UserSession
using Sklad_Kursach.Pages; // Чтобы видеть Inventory_Page и другие из корня

namespace Sklad_Kursach.Pages
{
    public partial class AdminHubPage : Page
    {
        public AdminHubPage()
        {
            InitializeComponent();
        }

        // --- НАВИГАЦИЯ ---

        private void GoProfile(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Profile_Page());
        }

        private void GoInventory(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Inventory_Page());
        }

        private void GoLogs(object sender, RoutedEventArgs e)
        {
            // Переход на страницу логов (она должна быть создана)
            NavigationService.Navigate(new Logs_Page());
        }

        private void GoIncoming(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Incoming_Page());
        }

        private void GoOutgoing(object sender, RoutedEventArgs e)
        {
            // Создай Outgoing_Page.xaml, если нет!
            //NavigationService.Navigate(new Outgoing_Page());
        }

        private void GoSorting(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Sort_Page());
        }

        private void GoWorkers(object sender, RoutedEventArgs e)
        {
            // В XAML Админа кнопка называлась GoWorkers, а страница называется Users_Page
            NavigationService.Navigate(new Users_Page());
        }

        private void Logout(object sender, RoutedEventArgs e)
        {
            //UserSession.UserID = null;
            //UserSession.UserRole = null;
            NavigationService.Navigate(new Auth_Page());
        }
    }
}