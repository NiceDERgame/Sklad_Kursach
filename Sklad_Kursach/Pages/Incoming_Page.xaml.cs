using Sklad_Kursach.Class;
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;

namespace Sklad_Kursach.Pages
{
    public partial class Incoming_Page : Page
    {
        public Incoming_Page()
        {
            InitializeComponent();
            CategoryCb.IsEditable = true;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var categories = new[] { "Еда", "Техника", "Химия", "Другое" };
            var suppliers = new[] { "ООО Фермер Про", "АО ТехноМир" };

            CategoryCb.ItemsSource = categories;
            SupplierCb.ItemsSource = suppliers;

            if (ReceiptDateDp.SelectedDate == null)
                ReceiptDateDp.SelectedDate = DateTime.Now.Date;

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
            bool categoryEmpty = string.IsNullOrWhiteSpace(CategoryCb.Text);
            bool supplierEmpty = string.IsNullOrWhiteSpace(SupplierCb.Text);

            if (string.IsNullOrWhiteSpace(Tovar_Name.Text) ||
                string.IsNullOrWhiteSpace(TotalCountTb.Text) ||
                string.IsNullOrWhiteSpace(PriceTb.Text) ||
                string.IsNullOrWhiteSpace(ShelfLifeTb.Text) ||
                categoryEmpty ||
                supplierEmpty ||
                ReceiptDateDp.SelectedDate == null)
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
                        cmd.CommandType = CommandType.StoredProcedure;

                        DateTime arrivalDateTime = ReceiptDateDp.SelectedDate.Value.Date + DateTime.Now.TimeOfDay;

                        cmd.Parameters.AddWithValue("@ProductName", Tovar_Name.Text.Trim());
                        cmd.Parameters.AddWithValue("@TypeID", CategoryCb.SelectedIndex + 1);
                        cmd.Parameters.AddWithValue("@ProviderID", SupplierCb.SelectedIndex + 1);
                        cmd.Parameters.AddWithValue("@EmployeeID", UserData.CurrentUser.EmployeeId);
                        cmd.Parameters.AddWithValue("@Quantity", int.Parse(TotalCountTb.Text));
                        cmd.Parameters.AddWithValue("@Price", decimal.Parse(PriceTb.Text));
                        cmd.Parameters.AddWithValue("@ShelfLifeHours", int.Parse(ShelfLifeTb.Text));
                        cmd.Parameters.AddWithValue("@ArrivalDate", arrivalDateTime);

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Товар успешно принят!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                NavigationService.GoBack();
            }
            catch (FormatException)
            {
                MessageBox.Show("Ошибка в числах. Проверьте формат цены, количества и срока хранения.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (SqlException ex)
            {
                MessageBox.Show($"Ошибка SQL:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка базы данных:\n{ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NumberValidation(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
        }
    }
}