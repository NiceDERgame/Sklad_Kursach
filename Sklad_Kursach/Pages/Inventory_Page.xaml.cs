using System;
using System.Collections.Generic;
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

        // Тестовая модель (потом заменишь на свою сущность)
        private class ProductRow
        {
            public string ProductName { get; set; }
            public string Category { get; set; }
            public DateTime DateReceipt { get; set; }
            public int TotalCount { get; set; }
            public string ShelfLife { get; set; }
            public string StorageCell { get; set; } // для списков “на складе” / “на отгрузку”
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            // Новый товар (без ячейки)
            NewItemsGrid.ItemsSource = new List<ProductRow>
            {
                new ProductRow { ProductName="Рис 5кг",   Category="Еда", DateReceipt=new DateTime(2026, 2, 5), TotalCount=20, ShelfLife="30 дней", StorageCell="—" },
                new ProductRow { ProductName="Сахар 1кг", Category="Еда", DateReceipt=new DateTime(2026, 2, 6), TotalCount=45, ShelfLife="60 дней", StorageCell="—" },
                new ProductRow { ProductName="Паста",     Category="Еда", DateReceipt=new DateTime(2026, 2, 7), TotalCount=18, ShelfLife="14 дней", StorageCell="—" }
            };

            // На складе (с ячейкой)
            InStockGrid.ItemsSource = new List<ProductRow>
            {
                new ProductRow { ProductName="Кабель HDMI",     Category="Техника", DateReceipt=new DateTime(2026, 1, 28), TotalCount=35, ShelfLife="90 дней",  StorageCell="B2" },
                new ProductRow { ProductName="Мышь офисная",    Category="Техника", DateReceipt=new DateTime(2026, 1, 15), TotalCount=12, ShelfLife="120 дней", StorageCell="C1" },
                new ProductRow { ProductName="Перчатки латекс", Category="Химия",   DateReceipt=new DateTime(2026, 1, 30), TotalCount=80, ShelfLife="45 дней",  StorageCell="A3" }
            };

            // Для отгрузки (тоже с ячейкой)
            ForShipmentGrid.ItemsSource = new List<ProductRow>
            {
                new ProductRow { ProductName="Бумага А4",         Category="Другое", DateReceipt=new DateTime(2026, 1, 20), TotalCount=10, ShelfLife="7 дней",  StorageCell="D1" },
                new ProductRow { ProductName="Антисептик 1л",     Category="Химия",  DateReceipt=new DateTime(2026, 1, 25), TotalCount=6,  ShelfLife="3 дня",   StorageCell="A1" },
                new ProductRow { ProductName="Скотч упаковочный", Category="Другое", DateReceipt=new DateTime(2026, 1, 22), TotalCount=15, ShelfLife="10 дней", StorageCell="C2" }
            };

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
