using Sklad_Kursach.Class;
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

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
            try
            {
                ZonesContainer.Children.Clear();

                string connStr = ConfigurationManager.ConnectionStrings["Warehouse_DB_V3"]?.ConnectionString;
                if (string.IsNullOrWhiteSpace(connStr))
                    throw new Exception("Строка подключения к БД не найдена.");

                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();

                    string query = @"
                        SELECT 
                            sc.Cell_id,
                            z.Name_Zona,
                            sc.CellCode
                        FROM StorageCell sc
                        JOIN Zona z ON sc.Zona_id = z.Zona_id
                        WHERE sc.Cell_id NOT IN (SELECT Cell_id FROM LotPlacement)
                        ORDER BY z.Name_Zona, sc.CellCode";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int cellId = Convert.ToInt32(reader["Cell_id"]);
                            string zoneName = reader["Name_Zona"].ToString();
                            string cellCode = reader["CellCode"].ToString();

                            Border card = new Border
                            {
                                Background = Brushes.White,
                                BorderBrush = (Brush)new BrushConverter().ConvertFromString("#DCE3EA"),
                                BorderThickness = new Thickness(1),
                                CornerRadius = new CornerRadius(12),
                                Margin = new Thickness(8),
                                Padding = new Thickness(14),
                                Effect = new DropShadowEffect
                                {
                                    BlurRadius = 14,
                                    ShadowDepth = 2,
                                    Opacity = 0.10
                                }
                            };

                            StackPanel panel = new StackPanel();

                            panel.Children.Add(new TextBlock
                            {
                                Text = zoneName,
                                FontSize = 15,
                                FontWeight = FontWeights.Bold,
                                Foreground = (Brush)new BrushConverter().ConvertFromString("#0D47A1"),
                                Margin = new Thickness(0, 0, 0, 4)
                            });

                            panel.Children.Add(new TextBlock
                            {
                                Text = "Ячейка: " + cellCode,
                                FontSize = 13,
                                Foreground = (Brush)new BrushConverter().ConvertFromString("#607D8B"),
                                Margin = new Thickness(0, 0, 0, 10)
                            });

                            Button cellButton = new Button
                            {
                                Content = "Разместить сюда",
                                Tag = cellId,
                                Height = 38,
                                Cursor = System.Windows.Input.Cursors.Hand,
                                Background = (Brush)new BrushConverter().ConvertFromString("#1565C0"),
                                Foreground = Brushes.White,
                                BorderThickness = new Thickness(0),
                                FontWeight = FontWeights.SemiBold
                            };

                            cellButton.Click += CellButton_Click;

                            cellButton.Template = BuildRoundedButtonTemplate();

                            panel.Children.Add(cellButton);
                            card.Child = panel;
                            ZonesContainer.Children.Add(card);
                        }
                    }
                }

                if (ZonesContainer.Children.Count == 0)
                {
                    ZonesContainer.Children.Add(new TextBlock
                    {
                        Text = "Нет свободных ячеек для размещения.",
                        FontSize = 15,
                        Foreground = Brushes.Gray,
                        Margin = new Thickness(10)
                    });
                }
            }
            catch (SqlException ex)
            {
                MessageBox.Show(
                    "Ошибка загрузки ячеек из базы данных:\n" + ex.Message,
                    "SQL ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Не удалось загрузить ячейки:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private ControlTemplate BuildRoundedButtonTemplate()
        {
            FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            FrameworkElementFactory presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenter.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);

            border.AppendChild(presenter);

            ControlTemplate template = new ControlTemplate(typeof(Button));
            template.VisualTree = border;
            return template;
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

                string connStr = ConfigurationManager.ConnectionStrings["Warehouse_DB_V3"]?.ConnectionString;
                if (string.IsNullOrWhiteSpace(connStr))
                    throw new Exception("Строка подключения к БД не найдена.");

                using (SqlConnection conn = new SqlConnection(connStr))
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

                        SqlCommand checkPlacedCmd = new SqlCommand(
                            "SELECT COUNT(*) FROM LotPlacement WHERE Lot_id = @Lot_id",
                            conn, transaction);
                        checkPlacedCmd.Parameters.AddWithValue("@Lot_id", _lotId);

                        int alreadyPlaced = Convert.ToInt32(checkPlacedCmd.ExecuteScalar());
                        if (alreadyPlaced > 0)
                            throw new Exception("Эта партия уже размещена в ячейке.");

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