using Sklad_Kursach.Class;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Sklad_Kursach.Pages
{
    public partial class Inventory_Page : Page
    {
        public class ProductRow
        {
            public int LotId { get; set; }
            public string ProductName { get; set; }
            public string Category { get; set; }
            public int TotalCount { get; set; }
            public string DateReceipt { get; set; }
            public string StorageCell { get; set; }
            public string DaysLeft { get; set; }
            public string FullInfo { get; set; }
        }

        private int _editingLotId = 0;

        public Inventory_Page()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!UserData.EnsureAuthorized(this))
                    return;

                if ((UserData.CurrentUser.Role ?? "") == "Администратор")
                    AdminControlPanel.Visibility = Visibility.Visible;
                else
                    AdminControlPanel.Visibility = Visibility.Collapsed;

                LoadData();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка загрузки страницы склада:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void LoadData()
        {
            try
            {
                string connStr = ConfigurationManager.ConnectionStrings["Warehouse_DB_V3"]?.ConnectionString;
                if (string.IsNullOrWhiteSpace(connStr))
                    throw new Exception("Строка подключения к БД не найдена.");

                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    // 1. НОВЫЙ ТОВАР
                    var newItems = new List<ProductRow>();
                    string sqlNew = @"
                        SELECT 
                            l.Lot_id,
                            p.[Name],
                            tt.Type_Tovar_Name,
                            l.TotalQuantity,
                            l.ArrivalDate,
                            l.ShelfLifeHours
                        FROM Lot l
                        JOIN Product p ON l.product_id = p.product_id
                        JOIN Type_Tovar tt ON p.Type_Tovar_id = tt.Type_Tovar_id
                        WHERE l.Lot_id NOT IN (SELECT Lot_id FROM LotPlacement)";

                    SqlCommand cmd = new SqlCommand(sqlNew, conn);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            DateTime arr = Convert.ToDateTime(r["ArrivalDate"]);
                            int hours = Convert.ToInt32(r["ShelfLifeHours"]);
                            TimeSpan left = arr.AddHours(hours) - DateTime.Now;

                            string daysLeftText = left.TotalSeconds <= 0
                                ? "ПРОСРОЧЕНО"
                                : $"{(int)left.TotalDays} дн. {left.Hours} ч.";

                            newItems.Add(new ProductRow
                            {
                                LotId = Convert.ToInt32(r["Lot_id"]),
                                ProductName = r["Name"].ToString(),
                                Category = r["Type_Tovar_Name"].ToString(),
                                TotalCount = Convert.ToInt32(r["TotalQuantity"]),
                                DaysLeft = daysLeftText,
                                FullInfo = $"Партия #{r["Lot_id"]}. Прибыл: {arr:dd.MM.yyyy HH:mm}"
                            });
                        }
                    }
                    NewItemsGrid.ItemsSource = newItems;

                    // 2. НА СКЛАДЕ
                    var stockItems = new List<ProductRow>();
                    string sqlStock = @"
                        SELECT 
                            l.Lot_id,
                            p.[Name],
                            sc.CellCode,
                            lp.Quantity,
                            l.ArrivalDate,
                            l.ShelfLifeHours
                        FROM LotPlacement lp
                        JOIN Lot l ON lp.Lot_id = l.Lot_id
                        JOIN Product p ON l.product_id = p.product_id
                        JOIN StorageCell sc ON lp.Cell_id = sc.Cell_id";

                    cmd = new SqlCommand(sqlStock, conn);
                    using (SqlDataReader r = cmd.ExecuteReader())
                    {
                        while (r.Read())
                        {
                            DateTime arr = Convert.ToDateTime(r["ArrivalDate"]);
                            int hours = Convert.ToInt32(r["ShelfLifeHours"]);
                            TimeSpan left = arr.AddHours(hours) - DateTime.Now;

                            string daysLeftText = left.TotalSeconds <= 0
                                ? "ПРОСРОЧЕНО"
                                : $"{(int)left.TotalDays} дн. {left.Hours} ч.";

                            stockItems.Add(new ProductRow
                            {
                                LotId = Convert.ToInt32(r["Lot_id"]),
                                ProductName = r["Name"].ToString(),
                                StorageCell = r["CellCode"].ToString(),
                                TotalCount = Convert.ToInt32(r["Quantity"]),
                                DaysLeft = daysLeftText,
                                FullInfo = $"Партия #{r["Lot_id"]}. Ячейка: {r["CellCode"]}. Прибыл: {arr:dd.MM.yyyy HH:mm}"
                            });
                        }
                    }
                    InStockGrid.ItemsSource = stockItems;
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(
                    "Ошибка загрузки товаров из базы данных:\n" + ex.Message,
                    "SQL ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось загрузить товары:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void NewItemsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (NewItemsGrid.SelectedItem != null)
                    InStockGrid.SelectedItem = null;

                SortBtn.IsEnabled = NewItemsGrid.SelectedItem != null;
            }
            catch
            {
            }
        }

        private void InfoBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ProductRow item = NewItemsGrid.SelectedItem as ProductRow ?? InStockGrid.SelectedItem as ProductRow;
                if (item != null)
                {
                    MessageBox.Show(
                        $"Товар: {item.ProductName}\n{item.FullInfo}\nОстаток срока: {item.DaysLeft}",
                        "Информация");
                }
                else
                {
                    MessageBox.Show("Выберите товар.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка отображения информации:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void SortBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ProductRow item = NewItemsGrid.SelectedItem as ProductRow;
                if (item != null)
                    NavigationService?.Navigate(new Sort_Page(item.LotId, item.ProductName));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка перехода на страницу сортировки:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void DeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ProductRow item = NewItemsGrid.SelectedItem as ProductRow ?? InStockGrid.SelectedItem as ProductRow;

                if (item == null)
                {
                    MessageBox.Show("Выберите товар для удаления.");
                    return;
                }

                if (MessageBox.Show(
                    $"Удалить партию '{item.ProductName}'? Это удалит всю историю товара.",
                    "Полное удаление",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning) != MessageBoxResult.Yes)
                {
                    return;
                }

                string connStr = ConfigurationManager.ConnectionStrings["Warehouse_DB_V3"]?.ConnectionString;
                if (string.IsNullOrWhiteSpace(connStr))
                    throw new Exception("Строка подключения к БД не найдена.");

                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    SqlTransaction transaction = conn.BeginTransaction();

                    try
                    {
                        SqlCommand cmdDelShip = new SqlCommand(
                            "DELETE FROM ShipmentPick WHERE Lot_id = @id", conn, transaction);
                        cmdDelShip.Parameters.AddWithValue("@id", item.LotId);
                        cmdDelShip.ExecuteNonQuery();

                        SqlCommand cmdDelPlace = new SqlCommand(
                            "DELETE FROM LotPlacement WHERE Lot_id = @id", conn, transaction);
                        cmdDelPlace.Parameters.AddWithValue("@id", item.LotId);
                        cmdDelPlace.ExecuteNonQuery();

                        SqlCommand cmdCleanLogs = new SqlCommand(
                            "DELETE FROM ActionLog WHERE Lot_id = @id", conn, transaction);
                        cmdCleanLogs.Parameters.AddWithValue("@id", item.LotId);
                        cmdCleanLogs.ExecuteNonQuery();

                        SqlCommand getRItem = new SqlCommand(
                            "SELECT ReceiptItem_id FROM Lot WHERE Lot_id = @id", conn, transaction);
                        getRItem.Parameters.AddWithValue("@id", item.LotId);
                        object rId = getRItem.ExecuteScalar();

                        SqlCommand cmdDelLot = new SqlCommand(
                            "DELETE FROM Lot WHERE Lot_id = @id", conn, transaction);
                        cmdDelLot.Parameters.AddWithValue("@id", item.LotId);
                        cmdDelLot.ExecuteNonQuery();

                        if (rId != null && rId != DBNull.Value)
                        {
                            SqlCommand cmdDelRItem = new SqlCommand(
                                "DELETE FROM ReceiptItem WHERE ReceiptItem_id = @rid", conn, transaction);
                            cmdDelRItem.Parameters.AddWithValue("@rid", Convert.ToInt32(rId));
                            cmdDelRItem.ExecuteNonQuery();
                        }

                        transaction.Commit();

                        MessageBox.Show("Товар и вся его история успешно удалены.");
                        LoadData();
                    }
                    catch
                    {
                        try { transaction.Rollback(); } catch { }
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось удалить товар.\nОшибка: " + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void EditProduct_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ProductRow item = NewItemsGrid.SelectedItem as ProductRow ?? InStockGrid.SelectedItem as ProductRow;

                if (item == null)
                {
                    MessageBox.Show("Выберите товар для редактирования.");
                    return;
                }

                _editingLotId = item.LotId;
                EditNameTb.Text = item.ProductName;
                EditCountTb.Text = item.TotalCount.ToString();

                LoadShelfLifeFromDB(_editingLotId);
                EditOverlay.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка открытия редактирования:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void LoadShelfLifeFromDB(int lotId)
        {
            try
            {
                string connStr = ConfigurationManager.ConnectionStrings["Warehouse_DB_V3"]?.ConnectionString;
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    SqlCommand cmd = new SqlCommand("SELECT ShelfLifeHours FROM Lot WHERE Lot_id = @id", conn);
                    cmd.Parameters.AddWithValue("@id", lotId);
                    object result = cmd.ExecuteScalar();
                    EditLifeTb.Text = result != null ? result.ToString() : "0";
                }
            }
            catch
            {
                EditLifeTb.Text = "0";
            }
        }

        private void CancelEdit_Click(object sender, RoutedEventArgs e)
        {
            EditOverlay.Visibility = Visibility.Collapsed;
            _editingLotId = 0;
        }

        private void SaveEdit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(EditNameTb.Text) ||
                    string.IsNullOrWhiteSpace(EditCountTb.Text) ||
                    string.IsNullOrWhiteSpace(EditLifeTb.Text))
                {
                    MessageBox.Show("Заполните все поля!");
                    return;
                }

                int newCount;
                int newLife;

                if (!int.TryParse(EditCountTb.Text, out newCount) ||
                    !int.TryParse(EditLifeTb.Text, out newLife))
                {
                    MessageBox.Show("Числа введены некорректно.");
                    return;
                }

                string connStr = ConfigurationManager.ConnectionStrings["Warehouse_DB_V3"]?.ConnectionString;
                if (string.IsNullOrWhiteSpace(connStr))
                    throw new Exception("Строка подключения к БД не найдена.");

                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    SqlTransaction transaction = conn.BeginTransaction();

                    try
                    {
                        string sqlGetProd = "SELECT product_id FROM Lot WHERE Lot_id = @lotId";
                        SqlCommand cmdGet = new SqlCommand(sqlGetProd, conn, transaction);
                        cmdGet.Parameters.AddWithValue("@lotId", _editingLotId);
                        object prodObj = cmdGet.ExecuteScalar();

                        if (prodObj == null || prodObj == DBNull.Value)
                            throw new Exception("Не найден product_id для партии.");

                        int prodId = Convert.ToInt32(prodObj);

                        string sqlUpdName = "UPDATE Product SET Name = @name WHERE product_id = @pid";
                        SqlCommand cmdName = new SqlCommand(sqlUpdName, conn, transaction);
                        cmdName.Parameters.AddWithValue("@name", EditNameTb.Text.Trim());
                        cmdName.Parameters.AddWithValue("@pid", prodId);
                        cmdName.ExecuteNonQuery();

                        string sqlUpdLot = "UPDATE Lot SET TotalQuantity = @qty, ShelfLifeHours = @life WHERE Lot_id = @lotId";
                        SqlCommand cmdLot = new SqlCommand(sqlUpdLot, conn, transaction);
                        cmdLot.Parameters.AddWithValue("@qty", newCount);
                        cmdLot.Parameters.AddWithValue("@life", newLife);
                        cmdLot.Parameters.AddWithValue("@lotId", _editingLotId);
                        cmdLot.ExecuteNonQuery();

                        string sqlUpdPlace = "UPDATE LotPlacement SET Quantity = @qty WHERE Lot_id = @lotId";
                        SqlCommand cmdPlace = new SqlCommand(sqlUpdPlace, conn, transaction);
                        cmdPlace.Parameters.AddWithValue("@qty", newCount);
                        cmdPlace.Parameters.AddWithValue("@lotId", _editingLotId);
                        cmdPlace.ExecuteNonQuery();

                        string sqlGetReceipt = "SELECT ReceiptItem_id FROM Lot WHERE Lot_id = @lotId";
                        SqlCommand cmdGetR = new SqlCommand(sqlGetReceipt, conn, transaction);
                        cmdGetR.Parameters.AddWithValue("@lotId", _editingLotId);
                        object rId = cmdGetR.ExecuteScalar();

                        if (rId != null && rId != DBNull.Value)
                        {
                            string sqlUpdR = "UPDATE ReceiptItem SET Quantity = @qty, ShelfLifeHours = @life WHERE ReceiptItem_id = @rid";
                            SqlCommand cmdR = new SqlCommand(sqlUpdR, conn, transaction);
                            cmdR.Parameters.AddWithValue("@qty", newCount);
                            cmdR.Parameters.AddWithValue("@life", newLife);
                            cmdR.Parameters.AddWithValue("@rid", Convert.ToInt32(rId));
                            cmdR.ExecuteNonQuery();
                        }

                        transaction.Commit();

                        MessageBox.Show("Товар успешно обновлен!");
                        EditOverlay.Visibility = Visibility.Collapsed;
                        LoadData();
                    }
                    catch
                    {
                        try { transaction.Rollback(); } catch { }
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка обновления: " + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void GoBack_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (NavigationService?.CanGoBack == true)
                    NavigationService.GoBack();
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