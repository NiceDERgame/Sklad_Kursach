using Sklad_Kursach.Class;
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;

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
            // Тестовые данные (потом заменить на БД)
            var categories = new[] { "Еда", "Техника", "Химия", "Другое" };
            var suppliers = new[] { "ООО Фермер Про", "АО ТехноМир" };

            CategoryCb.ItemsSource = categories;
            SupplierCb.ItemsSource = suppliers;

            if (ReceiptDateDp.SelectedDate == null)
                ReceiptDateDp.SelectedDate = System.DateTime.Now;

            // Применяем текущую раскладку сразу при загрузке
            ApplyResponsiveLayout();
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ApplyResponsiveLayout();
        }

        private void ApplyResponsiveLayout()
        {
            bool narrow = this.ActualWidth < 1100;

            if (narrow)
            {
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
            // Простая проверка на пустые поля
            if (string.IsNullOrWhiteSpace(Tovar_Name.Text) ||
                string.IsNullOrWhiteSpace(TotalCountTb.Text) ||
                string.IsNullOrWhiteSpace(PriceTb.Text) ||
                string.IsNullOrWhiteSpace(ShelfLifeTb.Text) ||
                CategoryCb.SelectedIndex == -1 ||
                SupplierCb.SelectedIndex == -1)
            {
                MessageBox.Show("Заполните все поля!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string connStr = ConfigurationManager.ConnectionStrings["Warehouse_DB_V3"].ConnectionString;

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    using (SqlCommand cmd = new SqlCommand("AddIncomingProduct", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure; // Обязательно указываем, что это процедура

                        cmd.Parameters.AddWithValue("@ProductName", Tovar_Name.Text);

                        // +1, так как в базе ID начинаются с 1, а в ComboBox с 0
                        cmd.Parameters.AddWithValue("@TypeID", CategoryCb.SelectedIndex + 1);
                        cmd.Parameters.AddWithValue("@ProviderID", SupplierCb.SelectedIndex + 1);
                        cmd.Parameters.AddWithValue("@EmployeeID", UserData.CurrentUser.AuthId);
                        cmd.Parameters.AddWithValue("@Quantity", int.Parse(TotalCountTb.Text));
                        cmd.Parameters.AddWithValue("@Price", decimal.Parse(PriceTb.Text));
                        cmd.Parameters.AddWithValue("@ShelfLifeHours", int.Parse(ShelfLifeTb.Text));
                        cmd.ExecuteNonQuery(); // Выполнение запроса
                    }
                    
                }

                MessageBox.Show("Товар успешно принят!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                NavigationService.GoBack();
            }
            catch (FormatException)
            {
                MessageBox.Show("Ошибка в числах (цена, количество). Проверьте формат.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка базы данных:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void NumberValidation(object sender, TextCompositionEventArgs e) // фигнюшка чтобы только цифры писались в ячейках
        {
            e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
        }
    }
}
