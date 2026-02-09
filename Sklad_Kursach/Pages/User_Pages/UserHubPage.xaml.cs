using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Sklad_Kursach.Class; // Для UserSession
using Sklad_Kursach.Pages; // Чтобы видеть Inventory_Page, Profile_Page и др.

// Если у тебя Users_Page лежит в Admins_Pages, но ты скопировал мой код с namespace Sklad_Kursach.Pages, то всё ок.
// Если нет, раскомментируй строку ниже:
// using Sklad_Kursach.Pages.Admins_Pages;

namespace Sklad_Kursach.Pages
{
    public partial class UserHubPage : Page
    {
        UserData userdata = new UserData();
        public UserHubPage()
        {
            InitializeComponent();

            // Скрываем кнопку "Пользователи", если это не Админ
            // Проверка на null нужна, чтобы не упало, если кнопки нет в XAML
            if (userdata.Role != "Администратор")
            {
                if (this.FindName("UsersButton") is Button btn)
                {
                    btn.Visibility = Visibility.Collapsed;
                }
            }
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

        private void GoIncoming(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Incoming_Page());
        }

        private void GoSorting(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new Sort_Page());
        }

        private void GoOutgoing(object sender, RoutedEventArgs e)
        {
            // Создай Outgoing_Page.xaml, если его нет!
            //NavigationService.Navigate(new Outgoing_Page());
        }

        private void GoUsers(object sender, RoutedEventArgs e)
        {
            // Эта кнопка видна только админу (через логику в конструкторе)
            NavigationService.Navigate(new Users_Page());
        }

        private void Logout(object sender, RoutedEventArgs e)
        {
            UserData.CurrentUser = null; // очистка юзера (сносит польностью все данные при выходе)
            NavigationService.Navigate(new Auth_Page());
        }
    }
}