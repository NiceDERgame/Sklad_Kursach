using Sklad_Kursach.Class;
using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Sklad_Kursach.Pages
{
    public partial class Sort_Page : Page
    {
        private readonly int _lotId;
        private readonly string _productName;

        public Sort_Page(int lotId, string productName)
        {
            InitializeComponent();
            _lotId = lotId;
            _productName = productName;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!UserData.EnsureAuthorized(this))
                    return;

                if (_lotId <= 0)
                {
                    MessageBox.Show(
                        "Неверный идентификатор партии.",
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                ProductTitle.Text = _productName;
                LoadZonesAndCells();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка загрузки страницы сортировки:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void LoadZonesAndCells()
        {
            ZonesContainer.Children.Clear();

            using (SqlConnection conn = new SqlConnection(UserData.GetConnectionString()))
            {
                conn.Open();

                string query = @"
                    SELECT 
                        sc.Cell_id,
                        z.ZoneName,
                        sc.CellCode
                    FROM StorageCell sc
                    JOIN Zona z ON sc.Zona_id = z.Zona_id
                    WHERE sc.Cell_id NOT IN (SELECT Cell_id FROM LotPlacement)
                    ORDER BY z.ZoneName, sc.CellCode";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int cellId = Convert.ToInt32(reader["Cell_id"]);
                        string zoneName = reader["ZoneName"].ToString();
                        string cellCode = reader["CellCode"].ToString();

                        Button cellButton = new Button
                        {
                            Content = zoneName + " / " + cellCode,
                            Tag = cellId,
                            Margin = new Thickness(5),
                            Padding = new Thickness(10),
                            Background = Brushes.White
                        };

                        cellButton.Click += CellButton_Click;
                        ZonesContainer.Children.Add(cellButton);
                    }
                }
            }
        }

        private void CellButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!UserData.EnsureAuthorized(this))
                    return;

                Button selectedButton = sender as Button;
                if (selectedButton == null || selectedButton.Tag == null)
                {
                    MessageBox.Show(
                        "Не удалось определить выбранную ячейку.",
                        "Ошибка",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return;
                }

                int cellId = Convert.ToInt32(selectedButton.Tag);

                using (SqlConnection conn = new SqlConnection(UserData.GetConnectionString()))
                {
                    conn.Open();
                    SqlTransaction transaction = conn.BeginTransaction();

                    try
                    {
                        SqlCommand qtyCmd = new SqlCommand(
                            "SELECT TotalQuantity FROM Lot WHERE Lot_id = @Lot_id",
                            conn, transaction);
                        qtyCmd.Parameters.AddWithValue("@Lot_id", _lotId);

                        object qtyObj = qtyCmd.ExecuteScalar();
                        if (qtyObj == null || qtyObj == DBNull.Value)
                            throw new Exception("Партия товара не найдена.");

                        int quantity = Convert.ToInt32(qtyObj);

                        SqlCommand insertPlacementCmd = new SqlCommand(@"
                            INSERT INTO LotPlacement (Lot_id, Cell_id, Quantity, PlacedByEmployee_id, PlacedAt)
                            VALUES (@Lot_id, @Cell_id, @Quantity, @Employee_id, GETDATE())",
                            conn, transaction);

                        insertPlacementCmd.Parameters.AddWithValue("@Lot_id", _lotId);
                        insertPlacementCmd.Parameters.AddWithValue("@Cell_id", cellId);
                        insertPlacementCmd.Parameters.AddWithValue("@Quantity", quantity);
                        insertPlacementCmd.Parameters.AddWithValue("@Employee_id", UserData.CurrentUser.EmployeeId);
                        insertPlacementCmd.ExecuteNonQuery();

                        SqlCommand logCmd = new SqlCommand(@"
                            INSERT INTO ActionLog (ActionTime, Employee_id, ActionType, Lot_id, Details)
                            VALUES (GETDATE(), @Employee_id, 'SORT', @Lot_id, @Details)",
                            conn, transaction);

                        logCmd.Parameters.AddWithValue("@Employee_id", UserData.CurrentUser.EmployeeId);
                        logCmd.Parameters.AddWithValue("@Lot_id", _lotId);
                        logCmd.Parameters.AddWithValue("@Details", $"Товар '{_productName}' размещён в ячейку.");
                        logCmd.ExecuteNonQuery();

                        transaction.Commit();
                    }
                    catch
                    {
                        try { transaction.Rollback(); } catch { }
                        throw;
                    }
                }

                MessageBox.Show(
                    "Товар успешно размещён на складе.",
                    "Успех",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                NavigationService?.Navigate(new Inventory_Page());
            }
            catch (SqlException ex)
            {
                MessageBox.Show(
                    "Ошибка базы данных при размещении товара:\n" + ex.Message,
                    "SQL ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка при размещении товара:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
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