using Sklad_Kursach.Class;
using Sklad_Kursach.Services;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;

namespace Sklad_Kursach.Pages
{
    public partial class Incoming_Page : Page
    {
        private readonly Dictionary<string, int> _categoryMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "Еда", 1 },
            { "Техника", 2 },
            { "Химия", 3 },
            { "Другое", 4 }
        };

        private readonly Dictionary<string, int> _supplierMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            { "ООО Фермер Про", 1 },
            { "ООО \"Фермер Про\"", 1 },
            { "АО ТехноМир", 2 },
            { "АО \"ТехноМир\"", 2 }
        };

        public Incoming_Page()
        {
            InitializeComponent();
            CategoryCb.IsEditable = true;
            SupplierCb.IsEditable = true;
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

            if (!_categoryMap.TryGetValue(CategoryCb.Text.Trim(), out int typeId))
            {
                MessageBox.Show("Выберите категорию из списка: Еда, Техника, Химия или Другое.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!_supplierMap.TryGetValue(SupplierCb.Text.Trim(), out int providerId))
            {
                MessageBox.Show("Выберите поставщика из списка.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(TotalCountTb.Text.Trim(), out int quantity) || quantity <= 0)
            {
                MessageBox.Show("Количество должно быть целым числом больше 0.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!decimal.TryParse(PriceTb.Text.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out decimal price))
            {
                if (!decimal.TryParse(PriceTb.Text.Trim(), NumberStyles.Number, new CultureInfo("ru-RU"), out price))
                {
                    MessageBox.Show("Цена указана в неверном формате.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            if (price <= 0)
            {
                MessageBox.Show("Цена должна быть больше 0.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(ShelfLifeTb.Text.Trim(), out int shelfLifeHours) || shelfLifeHours <= 0)
            {
                MessageBox.Show("Срок хранения должен быть целым числом больше 0.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string connStr = ConfigurationManager.ConnectionStrings["Warehouse_DB_V3"].ConnectionString;

            try
            {
                DateTime arrivalDateTime = ReceiptDateDp.SelectedDate.Value.Date + DateTime.Now.TimeOfDay;
                int createdReceiptId = 0;

                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    using (SqlCommand cmd = new SqlCommand("dbo.AddIncomingProduct", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.Add("@ProductName", SqlDbType.NVarChar, 100).Value = Tovar_Name.Text.Trim();
                        cmd.Parameters.Add("@TypeID", SqlDbType.Int).Value = typeId;
                        cmd.Parameters.Add("@ProviderID", SqlDbType.Int).Value = providerId;
                        cmd.Parameters.Add("@EmployeeID", SqlDbType.Int).Value = UserData.CurrentUser.EmployeeId;
                        cmd.Parameters.Add("@Quantity", SqlDbType.Int).Value = quantity;
                        cmd.Parameters.Add("@Price", SqlDbType.Decimal).Value = price;
                        cmd.Parameters["@Price"].Precision = 10;
                        cmd.Parameters["@Price"].Scale = 2;
                        cmd.Parameters.Add("@ShelfLifeHours", SqlDbType.Int).Value = shelfLifeHours;
                        cmd.Parameters.Add("@ArrivalDate", SqlDbType.DateTime).Value = arrivalDateTime;

                        object result = cmd.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                            createdReceiptId = Convert.ToInt32(result);
                    }
                }

                if (createdReceiptId <= 0)
                {
                    MessageBox.Show("Приёмка сохранена, но Receipt_id не вернулся из процедуры.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var invoiceItems = new List<ReceiptInvoiceItem>
                {
                    new ReceiptInvoiceItem
                    {
                        RowNumber = 1,
                        ProductName = Tovar_Name.Text.Trim(),
                        CategoryName = CategoryCb.Text.Trim(),
                        Quantity = quantity,
                        Price = price,
                        ShelfLifeHours = shelfLifeHours
                    }
                };

                string acceptedBy = GetCurrentUserFullName();
                string supplierName = SupplierCb.Text.Trim();
                string warehouseName = "Основной склад";

                string createdInvoicePath = GenerateReceiptInvoiceAfterSave(
                    connStr,
                    createdReceiptId,
                    supplierName,
                    warehouseName,
                    acceptedBy,
                    invoiceItems);

                MessageBox.Show(
                    "Товар успешно принят!\n\nНакладная создана:\n" + createdInvoicePath,
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                NavigationService?.GoBack();
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

        private string GenerateReceiptInvoiceAfterSave(
            string connStr,
            int receiptId,
            string supplierName,
            string warehouseName,
            string acceptedBy,
            List<ReceiptInvoiceItem> items)
        {
            string docNumber = $"PR-{DateTime.Now:yyyy}-{receiptId:D5}";

            var invoiceData = new ReceiptInvoiceData
            {
                ReceiptId = receiptId,
                DocumentNumber = docNumber,
                DocumentDate = DateTime.Now,
                SupplierName = string.IsNullOrWhiteSpace(supplierName) ? "Не указан" : supplierName,
                WarehouseName = string.IsNullOrWhiteSpace(warehouseName) ? "Основной склад" : warehouseName,
                AcceptedBy = string.IsNullOrWhiteSpace(acceptedBy) ? "Не указан" : acceptedBy,
                Items = items ?? new List<ReceiptInvoiceItem>()
            };

            string fullPath = ReceiptInvoiceService.CreateInvoiceDocx(invoiceData);
            SaveReceiptInvoiceInfoToDatabase(connStr, receiptId, docNumber, fullPath);

            return fullPath;
        }

        private void SaveReceiptInvoiceInfoToDatabase(string connStr, int receiptId, string documentNumber, string fullPath)
        {
            string fileName = Path.GetFileName(fullPath);

            string sql = @"
INSERT INTO dbo.ReceiptDocument
(
    Receipt_id,
    DocumentNumber,
    FileName,
    FilePath,
    CreatedAt,
    CreatedByEmployee_id
)
VALUES
(
    @ReceiptID,
    @DocumentNumber,
    @FileName,
    @FilePath,
    GETDATE(),
    @CreatedByEmployeeID
)";

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();

                using (SqlCommand cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.Add("@ReceiptID", SqlDbType.Int).Value = receiptId;
                    cmd.Parameters.Add("@DocumentNumber", SqlDbType.NVarChar, 50).Value = documentNumber;
                    cmd.Parameters.Add("@FileName", SqlDbType.NVarChar, 255).Value = fileName;
                    cmd.Parameters.Add("@FilePath", SqlDbType.NVarChar, 500).Value = fullPath;
                    cmd.Parameters.Add("@CreatedByEmployeeID", SqlDbType.Int).Value = UserData.CurrentUser.EmployeeId;

                    cmd.ExecuteNonQuery();
                }
            }
        }

        private string GetCurrentUserFullName()
        {
            try
            {
                if (UserData.CurrentUser == null)
                    return "Не указан";

                var type = UserData.CurrentUser.GetType();

                string lastName = type.GetProperty("LastName")?.GetValue(UserData.CurrentUser)?.ToString() ?? "";
                string firstName = type.GetProperty("FirstName")?.GetValue(UserData.CurrentUser)?.ToString() ?? "";
                string middleName = type.GetProperty("MiddleName")?.GetValue(UserData.CurrentUser)?.ToString() ?? "";

                string fullName = $"{lastName} {firstName} {middleName}".Trim();

                if (!string.IsNullOrWhiteSpace(fullName))
                    return fullName;

                string login = type.GetProperty("Login")?.GetValue(UserData.CurrentUser)?.ToString() ?? "";
                if (!string.IsNullOrWhiteSpace(login))
                    return login;
            }
            catch
            {
            }

            return "Не указан";
        }

        private void NumberValidation(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, "^[0-9]+$");
        }
    }
}