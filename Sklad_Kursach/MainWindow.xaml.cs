using System;
using System.Windows;
using Sklad_Kursach.Pages;

namespace Sklad_Kursach
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            try
            {
                MainFrame.Navigate(new Auth_Page());
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка запуска приложения: " + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}