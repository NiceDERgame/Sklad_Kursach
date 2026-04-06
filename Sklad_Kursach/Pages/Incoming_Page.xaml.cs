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
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!UserData.EnsureAuthorized(this))
                    return;

                if (ReceiptDateDp.SelectedDate == null)
                    ReceiptDateDp.SelectedDate = DateTime.Today;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка загрузки страницы приёмки:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
        }

        private void NumberValidation(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^[0-9]+([.,][0-9]+)?$");
        }

        private string GetSelectedComboText(ComboBox comboBox)
        {
            if (comboBox?.SelectedItem is ComboBoxItem item)
                return item.Content?.ToString()?.Trim();

            return comboBox?.Text?.Trim();
        }

        private (int receiptId, string receiptNumber) FindCreatedReceipt(
            string productName,
            int employeeId,
            DateTime arrivalDate,
            int quantity,
            decimal price)
        {
            using (SqlConnection conn = new SqlConnection(UserData.GetConnectionString()))
            {
                conn.Open();

                SqlCommand cmd = new SqlCommand(@"
                    SELECT TOP 1
                        r.Receipt_id,
                        r.ReceiptNumber
                    FROM Receipt r
                    JOIN ReceiptItem ri ON r.Receipt_id = ri.Receipt_id
                    JOIN Product p ON ri.product_id = p.product_id
                    WHERE r.employee_id = @empId
                      AND p.[Name] = @productName
                      AND ri.Quantity = @qty
                      AND ri.Price = @price
                      AND CAST(ri.ArrivalDate AS DATE) = CAST(@arrivalDate AS DATE)
                    ORDER BY r.Receipt_id DESC", conn);

                cmd.Parameters.AddWithValue("@empId", employeeId);
                cmd.Parameters.AddWithValue("@productName", productName);
                cmd.Parameters.AddWithValue("@qty", quantity);
                cmd.Parameters.AddWithValue("@price", price);
                cmd.Parameters.AddWithValue("@arrivalDate", arrivalDate);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return (
                            Convert.ToInt32(reader["Receipt_id"]),
                            reader["ReceiptNumber"].ToString()
                        );
                    }
                }
            }

            throw new Exception("Не удалось определить созданную накладную после приёмки.");
        }

        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            if (!UserData.EnsureAuthorized(this))
                return;

            try
            {
                string productName = Tovar_Name.Text?.Trim();
                string categoryName = GetSelectedComboText(CategoryCb);
                string supplierName = GetSelectedComboText(SupplierCb);
                string quantityText = TotalCountTb.Text?.Trim();
                string priceText = PriceTb.Text?.Trim();
                string shelfLifeText = ShelfLifeTb.Text?.Trim();
                DateTime? receiptDate = ReceiptDateDp.SelectedDate;

                if (string.IsNullOrWhiteSpace(productName) ||
                    string.IsNullOrWhiteSpace(categoryName) ||
                    string.IsNullOrWhiteSpace(supplierName) ||
                    string.IsNullOrWhiteSpace(quantityText) ||
                    string.IsNullOrWhiteSpace(priceText) ||
                    string.IsNullOrWhiteSpace(shelfLifeText) ||
                    receiptDate == null)
                {
                    MessageBox.Show(
                        "Заполните все поля.",
                        "Предупреждение",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                if (!_categoryMap.TryGetValue(categoryName, out int typeId))
                {
                    MessageBox.Show(
                        "Выбрана неизвестная категория.",
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                if (!_supplierMap.TryGetValue(supplierName, out int supplierId))
                {
                    MessageBox.Show(
                        "Выбран неизвестный поставщик.",
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(quantityText, out int quantity) || quantity <= 0)
                {
                    MessageBox.Show(
                        "Количество должно быть целым числом больше 0.",
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                decimal price;
                if (!decimal.TryParse(priceText, NumberStyles.Any, CultureInfo.InvariantCulture, out price) &&
                    !decimal.TryParse(priceText, NumberStyles.Any, CultureInfo.CurrentCulture, out price))
                {
                    MessageBox.Show(
                        "Некорректная цена.",
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                if (price <= 0)
                {
                    MessageBox.Show(
                        "Цена должна быть больше 0.",
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(shelfLifeText, out int shelfLifeHours) || shelfLifeHours <= 0)
                {
                    MessageBox.Show(
                        "Срок хранения должен быть целым числом больше 0.",
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                DateTime arrivalDate = receiptDate.Value.Date.Add(DateTime.Now.TimeOfDay);

                string connStr = ConfigurationManager.ConnectionStrings["Warehouse_DB_V3"]?.ConnectionString;
                if (string.IsNullOrWhiteSpace(connStr))
                    throw new Exception("Строка подключения к БД не найдена.");

                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    SqlCommand cmd = new SqlCommand("AddIncomingProduct", conn);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@ProductName", productName);
                    cmd.Parameters.AddWithValue("@TypeID", typeId);
                    cmd.Parameters.AddWithValue("@ProviderID", supplierId);
                    cmd.Parameters.AddWithValue("@EmployeeID", UserData.CurrentUser.EmployeeId);
                    cmd.Parameters.AddWithValue("@Quantity", quantity);
                    cmd.Parameters.AddWithValue("@Price", price);
                    cmd.Parameters.AddWithValue("@ShelfLifeHours", shelfLifeHours);
                    cmd.Parameters.AddWithValue("@ArrivalDate", arrivalDate);

                    cmd.ExecuteNonQuery();
                }

                int receiptId;
                string receiptNumber;

                try
                {
                    var receiptInfo = FindCreatedReceipt(
                        productName,
                        UserData.CurrentUser.EmployeeId,
                        arrivalDate,
                        quantity,
                        price);

                    receiptId = receiptInfo.receiptId;
                    receiptNumber = receiptInfo.receiptNumber;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Товар был принят, но не удалось определить созданную накладную:\n" + ex.Message,
                        "Предупреждение",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    ClearForm();
                    NavigateToHub();
                    return;
                }

                try
                {
                    ReceiptInvoiceData documentData = new ReceiptInvoiceData
                    {
                        DocumentNumber = receiptNumber,
                        ReceiptDate = arrivalDate,
                        SupplierName = supplierName,
                        EmployeeName = (UserData.CurrentUser.LastName + " " + UserData.CurrentUser.FirstName).Trim(),
                        TotalSum = quantity * price
                    };

                    documentData.Items.Add(new ReceiptInvoiceItem
                    {
                        ProductName = productName,
                        CategoryName = categoryName,
                        Quantity = quantity,
                        Price = price,
                        ShelfLifeHours = shelfLifeHours
                    });

                    string filePath = ReceiptInvoiceService.CreateInvoiceDocx(documentData);
                    string fileName = Path.GetFileName(filePath);

                    using (SqlConnection conn = new SqlConnection(UserData.GetConnectionString()))
                    {
                        conn.Open();

                        SqlCommand docCmd = new SqlCommand(@"
                            INSERT INTO ReceiptDocument
                                (Receipt_id, DocumentNumber, FileName, FilePath, CreatedAt, CreatedByEmployee_id)
                            VALUES
                                (@Receipt_id, @DocumentNumber, @FileName, @FilePath, GETDATE(), @CreatedByEmployee_id)", conn);

                        docCmd.Parameters.AddWithValue("@Receipt_id", receiptId);
                        docCmd.Parameters.AddWithValue("@DocumentNumber", receiptNumber);
                        docCmd.Parameters.AddWithValue("@FileName", fileName);
                        docCmd.Parameters.AddWithValue("@FilePath", filePath);
                        docCmd.Parameters.AddWithValue("@CreatedByEmployee_id", UserData.CurrentUser.EmployeeId);
                        docCmd.ExecuteNonQuery();
                    }

                    MessageBox.Show(
                        "Товар успешно принят, накладная сформирована.",
                        "Успех",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Товар принят, но накладную создать не удалось:\n" + ex.Message,
                        "Предупреждение",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }

                ClearForm();
                NavigateToHub();
            }
            catch (SqlException ex)
            {
                MessageBox.Show(
                    "Ошибка сохранения приёмки:\n" + ex.Message,
                    "SQL ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось сохранить приёмку:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void NavigateToHub()
        {
            string role = (UserData.CurrentUser?.Role ?? "").Trim();

            if (role == "Администратор" || role == "Старший рабочий")
                NavigationService?.Navigate(new AdminHubPage());
            else
                NavigationService?.Navigate(new UserHubPage());
        }

        private void ClearForm()
        {
            Tovar_Name.Text = string.Empty;
            CategoryCb.SelectedIndex = -1;
            SupplierCb.SelectedIndex = -1;
            TotalCountTb.Text = string.Empty;
            PriceTb.Text = string.Empty;
            ShelfLifeTb.Text = string.Empty;
            ReceiptDateDp.SelectedDate = DateTime.Today;
        }

        private void GoBack_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                NavigateToHub();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка возврата назад:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}