using System.Windows;
using Sklad_Kursach.Pages; // Подключите пространство имен Pages

namespace Sklad_Kursach
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            MainFrame.Navigate(new Auth_Page());
        }
    }
}