using Sklad_Kursach.Class;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace Sklad_Kursach.Pages
{
    public partial class Inventory_Page : Page
    {
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

                LoadData();

                if (AdminControlPanel != null)
                {
                    string role = UserData.CurrentUser.Role ?? string.Empty;
                    AdminControlPanel.Visibility = role == "Администратор"
                        ? Visibility.Visible
                        : Visibility.Collapsed;
                }
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
            using (SqlConnection conn = new SqlConnection(UserData.GetConnectionString()))
            {
                conn.Open();

                string newItemsSql = @"
                    SELECT 
                        l.Lot_id,
                        p.[Name] AS [Название],
                        t.Type_Tovar_Name AS [Категория],
                        l.TotalQuantity AS [Количество],
                        CASE 
                            WHEN DATEADD(hour, l.ShelfLifeHours, CAST(l.ArrivalDate AS DATETIME)) < GETDATE() THEN 'ПРОСРОЧЕНО'
                            ELSE CONVERT(varchar, DATEDIFF(hour, GETDATE(), DATEADD(hour, l.ShelfLifeHours, CAST(l.ArrivalDate AS DATETIME)))) + ' ч.'
                        END AS [Остаток срока]
                    FROM Lot l
                    JOIN Product p ON l.product_id = p.product_id
                    JOIN Type_Tovar t ON p.Type_Tovar_id = t.Type_Tovar_id
                    WHERE l.Lot_id NOT IN (SELECT Lot_id FROM LotPlacement)";

                SqlDataAdapter newAdapter = new SqlDataAdapter(newItemsSql, conn);
                DataTable newTable = new DataTable();
                newAdapter.Fill(newTable);
                NewItemsGrid.ItemsSource = newTable.DefaultView;

                string stockSql = @"
                    SELECT 
                        l.Lot_id,
                        p.[Name] AS [Название],
                        t.Type_Tovar_Name AS [Категория],
                        lp.Quantity AS [Количество],
                        sc.CellCode AS [Ячейка],
                        CASE 
                            WHEN DATEADD(hour, l.ShelfLifeHours, CAST(l.ArrivalDate AS DATETIME)) < GETDATE() THEN 'ПРОСРОЧЕНО'
                            ELSE CONVERT(varchar, DATEDIFF(hour, GETDATE(), DATEADD(hour, l.ShelfLifeHours, CAST(l.ArrivalDate AS DATETIME)))) + ' ч.'
                        END AS [Остаток срока]
                    FROM LotPlacement lp
                    JOIN Lot l ON lp.Lot_id = l.Lot_id
                    JOIN Product p ON l.product_id = p.product_id
                    JOIN Type_Tovar t ON p.Type_Tovar_id = t.Type_Tovar_id
                    JOIN StorageCell sc ON lp.Cell_id = sc.Cell_id";

                SqlDataAdapter stockAdapter = new SqlDataAdapter(stockSql, conn);
                DataTable stockTable = new DataTable();
                stockAdapter.Fill(stockTable);
                InStockGrid.ItemsSource = stockTable.DefaultView;
            }
        }

        private void NewItemsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void InfoBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataRowView row = InStockGrid.SelectedItem as DataRowView ?? NewItemsGrid.SelectedItem as DataRowView;
                if (row == null)
                {
                    MessageBox.Show(
                        "Выберите товар для просмотра информации.",
                        "Информация",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                string info = "";
                foreach (DataColumn col in row.Row.Table.Columns)
                {
                    info += col.ColumnName + ": " + row[col.ColumnName] + "\n";
                }

                MessageBox.Show(
                    info,
                    "Информация о товаре",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
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

        private void AddProduct_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataRowView row = NewItemsGrid.SelectedItem as DataRowView;
                if (row == null)
                {
                    MessageBox.Show(
                        "Выберите товар из списка новых товаров.",
                        "Предупреждение",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                int lotId = Convert.ToInt32(row["Lot_id"]);
                string productName = Convert.ToString(row["Название"]);

                NavigationService?.Navigate(new Sort_Page(lotId, productName));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка перехода к размещению товара:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void SortBtn_Click(object sender, RoutedEventArgs e)
        {
            AddProduct_Click(sender, e);
        }

        private void EditProduct_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Редактирование товара оставлено без изменений. Если у тебя в старом проекте уже был рабочий способ редактирования, оставь именно его.",
                "Информация",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void CancelEdit_Click(object sender, RoutedEventArgs e)
        {
        }

        private void SaveEdit_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Сохранение редактирования пока оставлено без изменений. Если у тебя в старом коде был рабочий вариант — верни его в этот обработчик.",
                "Информация",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void DeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DataRowView row = InStockGrid.SelectedItem as DataRowView;
                if (row == null)
                {
                    MessageBox.Show(
                        "Выберите товар для удаления.",
                        "Предупреждение",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }

                int lotId = Convert.ToInt32(row["Lot_id"]);

                if (MessageBox.Show(
                    "Удалить выбранный товар?",
                    "Подтверждение",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) != MessageBoxResult.Yes)
                {
                    return;
                }

                using (SqlConnection conn = new SqlConnection(UserData.GetConnectionString()))
                {
                    conn.Open();
                    SqlTransaction transaction = conn.BeginTransaction();

                    try
                    {
                        int? receiptItemId = null;

                        SqlCommand getReceiptItemCmd = new SqlCommand(
                            "SELECT ReceiptItem_id FROM Lot WHERE Lot_id = @Lot_id",
                            conn, transaction);
                        getReceiptItemCmd.Parameters.AddWithValue("@Lot_id", lotId);

                        object receiptItemObj = getReceiptItemCmd.ExecuteScalar();
                        if (receiptItemObj != null && receiptItemObj != DBNull.Value)
                            receiptItemId = Convert.ToInt32(receiptItemObj);

                        SqlCommand deleteShipmentPickCmd = new SqlCommand(
                            "DELETE FROM ShipmentPick WHERE Lot_id = @Lot_id",
                            conn, transaction);
                        deleteShipmentPickCmd.Parameters.AddWithValue("@Lot_id", lotId);
                        deleteShipmentPickCmd.ExecuteNonQuery();

                        SqlCommand deleteLotPlacementCmd = new SqlCommand(
                            "DELETE FROM LotPlacement WHERE Lot_id = @Lot_id",
                            conn, transaction);
                        deleteLotPlacementCmd.Parameters.AddWithValue("@Lot_id", lotId);
                        deleteLotPlacementCmd.ExecuteNonQuery();

                        SqlCommand deleteActionLogCmd = new SqlCommand(
                            "DELETE FROM ActionLog WHERE Lot_id = @Lot_id",
                            conn, transaction);
                        deleteActionLogCmd.Parameters.AddWithValue("@Lot_id", lotId);
                        deleteActionLogCmd.ExecuteNonQuery();

                        SqlCommand deleteLotCmd = new SqlCommand(
                            "DELETE FROM Lot WHERE Lot_id = @Lot_id",
                            conn, transaction);
                        deleteLotCmd.Parameters.AddWithValue("@Lot_id", lotId);
                        deleteLotCmd.ExecuteNonQuery();

                        if (receiptItemId.HasValue)
                        {
                            SqlCommand deleteReceiptItemCmd = new SqlCommand(
                                "DELETE FROM ReceiptItem WHERE ReceiptItem_id = @ReceiptItem_id",
                                conn, transaction);
                            deleteReceiptItemCmd.Parameters.AddWithValue("@ReceiptItem_id", receiptItemId.Value);
                            deleteReceiptItemCmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        try { transaction.Rollback(); } catch { }
                        throw;
                    }
                }

                LoadData();

                MessageBox.Show(
                    "Товар удалён.",
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (SqlException ex)
            {
                MessageBox.Show(
                    "Ошибка базы данных при удалении товара:\n" + ex.Message,
                    "SQL ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка при удалении товара:\n" + ex.Message,
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