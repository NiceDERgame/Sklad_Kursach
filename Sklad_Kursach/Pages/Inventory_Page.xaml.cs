using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace Sklad_Kursach.Pages
{
    public partial class Inventory_Page : Page
    {
        public class ProductRow
        {
            public string ProductName { get; set; }
            public string Category { get; set; }

            public string DateReceipt { get; set; }
            public int TotalCount { get; set; }

            public string ShelfLife { get; set; }
            public string StorageCell { get; set; }

        }

        public Inventory_Page()
        {
            InitializeComponent();
            string connStr = ConfigurationManager.ConnectionStrings["Warehouse_DB_V3"].ConnectionString;

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();

                // ========================================================
                // 1. НОВЫЙ ТОВАР (Товары в Lot, которых еще нет в ячейках)
                // ========================================================
                string queryNew = @"
            SELECT 
                p.[Name] AS ProductName,
                tt.Type_Tovar_Name AS Category,
                l.ArrivalDate,
                l.TotalQuantity,
                l.ShelfLifeHours
            FROM dbo.Lot l
            JOIN dbo.Product p ON l.product_id = p.product_id
            JOIN dbo.Type_Tovar tt ON p.type_Tovar_id = tt.type_Tovar_id
            -- Исключаем те, которые уже положили в ячейки
            WHERE l.Lot_id NOT IN (SELECT Lot_id FROM dbo.LotPlacement);";

                SqlCommand cmdNew = new SqlCommand(queryNew, conn);
                List<ProductRow> newItemsList = new List<ProductRow>();

                using (SqlDataReader reader = cmdNew.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        newItemsList.Add(new ProductRow
                        {
                            ProductName = reader["ProductName"].ToString(),
                            Category = reader["Category"].ToString(),
                            DateReceipt = Convert.ToDateTime(reader["ArrivalDate"]).ToShortDateString(),
                            TotalCount = Convert.ToInt32(reader["TotalQuantity"]),
                            ShelfLife = reader["ShelfLifeHours"].ToString() + " ч.",
                            StorageCell = "—" // Ячейки еще нет
                        });
                    }
                }
                NewItemsGrid.ItemsSource = newItemsList;

                // ========================================================
                // 2. НА СКЛАДЕ (Товары, которые лежат в ячейках)
                // ========================================================
                string queryInStock = @"
                           SELECT 
                               p.[Name] AS ProductName,
                               tt.Type_Tovar_Name AS Category,
                               l.ArrivalDate,
                               lp.Quantity AS TotalCount,
                               l.ShelfLifeHours,
                               sc.CellCode AS StorageCell
                           FROM dbo.LotPlacement lp
                           JOIN dbo.Lot l ON lp.Lot_id = l.Lot_id
                           JOIN dbo.Product p ON l.product_id = p.product_id
                           JOIN dbo.Type_Tovar tt ON p.type_Tovar_id = tt.type_Tovar_id
                           JOIN dbo.StorageCell sc ON lp.Cell_id = sc.Cell_id;";

                SqlCommand cmdInStock = new SqlCommand(queryInStock, conn);
                List<ProductRow> inStockList = new List<ProductRow>();

                using (SqlDataReader reader = cmdInStock.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        inStockList.Add(new ProductRow
                        {
                            ProductName = reader["ProductName"].ToString(),
                            Category = reader["Category"].ToString(),
                            DateReceipt = Convert.ToDateTime(reader["ArrivalDate"]).ToShortDateString(),
                            TotalCount = Convert.ToInt32(reader["TotalCount"]),
                            ShelfLife = reader["ShelfLifeHours"].ToString() + " ч.",
                            StorageCell = reader["StorageCell"].ToString()
                        });
                    }
                }
                InStockGrid.ItemsSource = inStockList;

            //    // ========================================================
            //    // 3. ДЛЯ ОТГРУЗКИ (Товары, которые собирают по накладной)
            //    // ========================================================
            //    string queryForShipment = @"
            //SELECT 
            //    p.[Name] AS ProductName,
            //    tt.Type_Tovar_Name AS Category,
            //    l.ArrivalDate,
            //    sp.Quantity AS TotalCount,
            //    l.ShelfLifeHours,
            //    sc.CellCode AS StorageCell
            //FROM dbo.ShipmentPick sp
            //JOIN dbo.Lot l ON sp.Lot_id = l.Lot_id
            //JOIN dbo.Product p ON l.product_id = p.product_id
            //JOIN dbo.Type_Tovar tt ON p.type_Tovar_id = tt.type_Tovar_id
            //JOIN dbo.StorageCell sc ON sp.Cell_id = sc.Cell_id;";

            //    SqlCommand cmdForShipment = new SqlCommand(queryForShipment, conn);
            //    List<ProductRow> forShipmentList = new List<ProductRow>();

            //    using (SqlDataReader reader = cmdForShipment.ExecuteReader())
            //    {
            //        while (reader.Read())
            //        {
            //            forShipmentList.Add(new ProductRow
            //            {
            //                ProductName = reader["ProductName"].ToString(),
            //                Category = reader["Category"].ToString(),
            //                DateReceipt = Convert.ToDateTime(reader["ArrivalDate"]).ToShortDateString(),
            //                TotalCount = Convert.ToInt32(reader["TotalCount"]),
            //                ShelfLife = reader["ShelfLifeHours"].ToString() + " ч.",
            //                StorageCell = reader["StorageCell"].ToString()
            //            });
            //        }
            //    }
            //    ForShipmentGrid.ItemsSource = forShipmentList;
            }
        }
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {


            {
                //// Новый товар (без ячейки)
                //NewItemsGrid.ItemsSource = new List<ProductRow>
                //{
                //    new ProductRow { ProductName="Рис 5кг",   Category="Еда", DateReceipt=new DateTime(2026, 2, 5), TotalCount=20, ShelfLife="30 дней", StorageCell="—" },
                //    new ProductRow { ProductName="Сахар 1кг", Category="Еда", DateReceipt=new DateTime(2026, 2, 6), TotalCount=45, ShelfLife="60 дней", StorageCell="—" },
                //    new ProductRow { ProductName="Паста",     Category="Еда", DateReceipt=new DateTime(2026, 2, 7), TotalCount=18, ShelfLife="14 дней", StorageCell="—" }
                //};

                //// На складе (с ячейкой)
                //InStockGrid.ItemsSource = new List<ProductRow>
                //{
                //    new ProductRow { ProductName="Кабель HDMI",     Category="Техника", DateReceipt=new DateTime(2026, 1, 28), TotalCount=35, ShelfLife="90 дней",  StorageCell="B2" },
                //    new ProductRow { ProductName="Мышь офисная",    Category="Техника", DateReceipt=new DateTime(2026, 1, 15), TotalCount=12, ShelfLife="120 дней", StorageCell="C1" },
                //    new ProductRow { ProductName="Перчатки латекс", Category="Химия",   DateReceipt=new DateTime(2026, 1, 30), TotalCount=80, ShelfLife="45 дней",  StorageCell="A3" }
                //};

                //// Для отгрузки (тоже с ячейкой)
                //ForShipmentGrid.ItemsSource = new List<ProductRow>
                //{
                //    new ProductRow { ProductName="Бумага А4",         Category="Другое", DateReceipt=new DateTime(2026, 1, 20), TotalCount=10, ShelfLife="7 дней",  StorageCell="D1" },
                //    new ProductRow { ProductName="Антисептик 1л",     Category="Химия",  DateReceipt=new DateTime(2026, 1, 25), TotalCount=6,  ShelfLife="3 дня",   StorageCell="A1" },
                //    new ProductRow { ProductName="Скотч упаковочный", Category="Другое", DateReceipt=new DateTime(2026, 1, 22), TotalCount=15, ShelfLife="10 дней", StorageCell="C2" }
                //};
            }
            InfoBtn.IsEnabled = false;
            SortBtn.IsEnabled = false;
        }

        private void NewItemsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool selected = NewItemsGrid.SelectedItem != null;
            InfoBtn.IsEnabled = selected;
            SortBtn.IsEnabled = selected;
        }

        private void InfoBtn_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Информация (заглушка).");
        }

        private void SortBtn_Click(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new Sort_Page());
        }

        private void GoBack_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService?.CanGoBack == true)
                NavigationService.GoBack();
        }

        // Заглушки админ-кнопок (чтобы не ругалось)
        private void AddProduct_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Добавить (заглушка)");
        private void EditProduct_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Редактировать (заглушка)");
        private void DeleteProduct_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Удалить (заглушка)");
    }
}
