using Sklad_Kursach.Class;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Navigation;

namespace Sklad_Kursach.Pages
{
    public partial class Sort_Page : Page
    {
        private int _lotId;
        private string _prodName;
        private string connStr = ConfigurationManager.ConnectionStrings["Warehouse_DB_V3"].ConnectionString;

        // Конструктор по умолчанию (чтобы не было ошибок в XAML)
        public Sort_Page() : this(0, "") { }

        // Конструктор, который принимает данные о товаре
        public Sort_Page(int lotId, string prodName)
        {
            InitializeComponent();
            _lotId = lotId;
            _prodName = prodName;

            if (_lotId > 0)
                ProductTitle.Text = $"Куда положить товар: {_prodName}?";
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            LoadZonesAndCells();
        }

        // 1. ГЕНЕРАЦИЯ КНОПОК (ИНТЕРФЕЙС)
        private void LoadZonesAndCells()
        {
            ZonesContainer.Children.Clear(); // Очищаем контейнер перед загрузкой

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();

                // Используем List, чтобы сохранить данные и закрыть DataReader, 
                // иначе нельзя будет делать вложенные запросы для ячеек.
                var zones = new List<Tuple<int, string>>();
                SqlCommand cmdZones = new SqlCommand("SELECT Zona_id, Name_Zona FROM Zona", conn);

                using (var reader = cmdZones.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        zones.Add(new Tuple<int, string>(reader.GetInt32(0), reader.GetString(1)));
                    }
                }

                // Шаг 2: Для каждой зоны рисуем карточку и загружаем ячейки
                foreach (var zone in zones)
                {
                    int zoneId = zone.Item1;
                    string zoneName = zone.Item2;

                    Border card = new Border
                    {
                        Background = Brushes.White,
                        CornerRadius = new CornerRadius(10),
                        Padding = new Thickness(20),
                        Margin = new Thickness(0, 0, 0, 20),
                        BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom("#E0E0E0"),
                        BorderThickness = new Thickness(1)
                    };
                    card.Effect = new DropShadowEffect { BlurRadius = 15, Opacity = 0.1, ShadowDepth = 2, Color = Colors.Black };

                    StackPanel stack = new StackPanel();

                    // Заголовок зоны (например "Зона А (Еда)")
                    TextBlock header = new TextBlock
                    {
                        Text = zoneName,
                        FontSize = 18,
                        FontWeight = FontWeights.Bold,
                        Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#1565C0"),
                        Margin = new Thickness(0, 0, 0, 15)
                    };

                    // Панель для кнопок (WrapPanel чтобы кнопки переносились на новую строку)
                    WrapPanel buttonsPanel = new WrapPanel { Orientation = Orientation.Horizontal };

                    // Собираем всё вместе
                    stack.Children.Add(header);
                    stack.Children.Add(buttonsPanel);
                    card.Child = stack;

                    // Загружаем кнопки ячеек внутрь buttonsPanel
                    LoadCellsForZone(conn, zoneId, buttonsPanel);

                    // Добавляем готовую карточку на экран
                    ZonesContainer.Children.Add(card);
                }
            }
        }

        private void LoadCellsForZone(SqlConnection conn, int zoneId, WrapPanel panel)
        {
            SqlCommand cmdCells = new SqlCommand("SELECT CellCode FROM StorageCell WHERE Zona_id = @zid", conn);
            cmdCells.Parameters.AddWithValue("@zid", zoneId);

            using (var r = cmdCells.ExecuteReader())
            {
                bool hasCells = false;
                while (r.Read())
                {
                    hasCells = true;
                    string code = r["CellCode"].ToString(); // Например "A-01"

                    // Создаем кнопку
                    Button btn = new Button
                    {
                        Content = code,
                        Width = 90,
                        Height = 45,
                        Margin = new Thickness(5),
                        Background = Brushes.Transparent,
                        BorderBrush = (SolidColorBrush)new BrushConverter().ConvertFrom("#B0BEC5"),
                        BorderThickness = new Thickness(1),
                        FontSize = 14,
                        FontWeight = FontWeights.SemiBold,
                        Cursor = System.Windows.Input.Cursors.Hand
                    };

                    // Привязываем событие нажатия
                    btn.Click += Cell_Click;

                    panel.Children.Add(btn);
                }

                if (!hasCells)
                {
                    panel.Children.Add(new TextBlock { Text = "Нет ячеек", Foreground = Brushes.Gray, FontStyle = FontStyles.Italic });
                }
            }
        }

        // 2. ЛОГИКА СОХРАНЕНИЯ (ТРАНЗАКЦИЯ)

        private void Cell_Click(object sender, RoutedEventArgs e)
        {
            if (_lotId == 0)
            {
                MessageBox.Show("Ошибка: Товар не выбран! Вернитесь на склад.");
                return;
            }

            var btn = sender as Button;
            string cellCode = btn.Content.ToString(); // Получаем точный код (A-01)

            SaveSorting(cellCode);
        }

        private void SaveSorting(string cellCode)
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                // Начинаем транзакцию: Всё или ничего
                SqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    // 1. Ищем ID ячейки по коду
                    SqlCommand cmdCell = new SqlCommand("SELECT Cell_id FROM StorageCell WHERE CellCode = @code", conn, transaction);
                    cmdCell.Parameters.AddWithValue("@code", cellCode);
                    object res = cmdCell.ExecuteScalar();

                    if (res == null)
                    {
                        transaction.Rollback();
                        MessageBox.Show($"Ошибка: Ячейка '{cellCode}' не найдена в базе данных!");
                        return;
                    }
                    int cellId = (int)res;

                    // 2. Перемещаем товар (Копируем из Lot в LotPlacement)
                    // Используем IF NOT EXISTS для защиты от дублей
                    string sqlPlace = @"
                        INSERT INTO LotPlacement (Lot_id, Cell_id, Quantity, PlacedByEmployee_id)
                        SELECT Lot_id, @cellId, TotalQuantity, @empId
                        FROM Lot 
                        WHERE Lot_id = @lotId";

                    SqlCommand cmdPlace = new SqlCommand(sqlPlace, conn, transaction);
                    cmdPlace.Parameters.AddWithValue("@cellId", cellId);
                    cmdPlace.Parameters.AddWithValue("@empId", UserData.CurrentUser.EmployeeId);
                    cmdPlace.Parameters.AddWithValue("@lotId", _lotId);

                    int rows = cmdPlace.ExecuteNonQuery();

                    if (rows == 0)
                    {
                        transaction.Rollback();
                        MessageBox.Show("Ошибка: Не удалось переместить товар. Возможно, он уже был распределен.");
                        return;
                    }

                    // 3. Пишем ЛОГ
                    string sqlLog = @"INSERT INTO ActionLog (Employee_id, ActionType, Lot_id, Cell_id, Details)
                                      VALUES (@empId, 'SORT', @lotId, @cellId, N'Товар ' + @pName + ' размещен в ячейку ' + @code)";

                    SqlCommand cmdLog = new SqlCommand(sqlLog, conn, transaction);
                    cmdLog.Parameters.AddWithValue("@empId", UserData.CurrentUser.EmployeeId);
                    cmdLog.Parameters.AddWithValue("@lotId", _lotId);
                    cmdLog.Parameters.AddWithValue("@cellId", cellId);
                    cmdLog.Parameters.AddWithValue("@pName", _prodName);
                    cmdLog.Parameters.AddWithValue("@code", cellCode);
                    cmdLog.ExecuteNonQuery();

                    // Все прошло успешно -> Сохраняем
                    transaction.Commit();

                    MessageBox.Show($"Успех! Товар '{_prodName}' перемещен в ячейку {cellCode}.");

                    // Возвращаемся на склад
                    NavigationService.Navigate(new Inventory_Page());
                }
                catch (Exception ex)
                {
                    transaction.Rollback(); // Отменяем изменения при ошибке
                    MessageBox.Show("Ошибка базы данных: " + ex.Message);
                }
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack) NavigationService.GoBack();
        }
    }
}