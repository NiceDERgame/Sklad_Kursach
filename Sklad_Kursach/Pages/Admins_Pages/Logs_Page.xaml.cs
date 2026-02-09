using Sklad_Kursach.Class;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Sklad_Kursach.Pages
{
    public partial class Logs_Page : Page
    {
        public Logs_Page()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // TODO: Загрузка логов
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
    }
}