using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Sklad_Kursach.Pages
{
    public partial class Incoming_Page : Page
    {
        public Incoming_Page()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Тестовые данные (потом заменишь на БД)
            var categories = new[] { "Еда", "Техника", "Химия", "Другое" };
            var suppliers = new[] { "ООО Поставщик-1", "ИП Поставщик-2", "Компания Снабжение" };

            CategoryCb.ItemsSource = categories;
            SupplierCb.ItemsSource = suppliers;

            if (ReceiptDateDp.SelectedDate == null)
                ReceiptDateDp.SelectedDate = System.DateTime.Today;

            // Применяем текущую раскладку сразу при загрузке
            ApplyResponsiveLayout();
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ApplyResponsiveLayout();
        }

        private void ApplyResponsiveLayout()
        {
            // Порог — под твой проект (можешь 1050/1100/1150 подобрать)
            bool narrow = this.ActualWidth < 1100;

            if (narrow)
            {
                // 1 колонка: Форма сверху, Инструкция снизу
                Grid.SetRow(FormCard, 0);
                Grid.SetColumn(FormCard, 0);
                Grid.SetColumnSpan(FormCard, 3);

                Grid.SetRow(HelpCard, 1);
                Grid.SetColumn(HelpCard, 0);
                Grid.SetColumnSpan(HelpCard, 3);

                HelpCard.Margin = new Thickness(0, 18, 0, 0);
                FormCard.Margin = new Thickness(0);
            }
            else
            {
                // 2 колонки: Инструкция слева, Форма справа
                Grid.SetRow(HelpCard, 0);
                Grid.SetColumn(HelpCard, 0);
                Grid.SetColumnSpan(HelpCard, 1);

                Grid.SetRow(FormCard, 0);
                Grid.SetColumn(FormCard, 2);
                Grid.SetColumnSpan(FormCard, 1);

                HelpCard.Margin = new Thickness(0);
                FormCard.Margin = new Thickness(0);
            }
        }

        private void GoBack_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService?.CanGoBack == true)
                NavigationService.GoBack();
        }

        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Принято (заглушка). Здесь потом добавишь сохранение в БД.");
        }

        // Только цифры
        private void NumberValidation(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
        }
    }
}
