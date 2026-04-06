using Sklad_Kursach.Class;
using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace Sklad_Kursach.Pages
{
    public partial class AdminHubPage : Page
    {
        public AdminHubPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!UserData.EnsureAuthorized(this))
                    return;

                LoadStats();
                UserData.LoadAvatar(UserData.CurrentUser.AuthId, AvatarBorder, AvatarEmoji);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка загрузки страницы:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void LoadStats()
        {
            using (SqlConnection conn = new SqlConnection(UserData.GetConnectionString()))
            {
                conn.Open();

                SqlCommand cmdTotal = new SqlCommand("SELECT ISNULL(SUM(Quantity), 0) FROM LotPlacement", conn);
                TotalItemsTb.Text = Convert.ToString(cmdTotal.ExecuteScalar());

                SqlCommand cmdNew = new SqlCommand(
                    "SELECT COUNT(*) FROM ActionLog WHERE ActionType='INCOMING' AND CAST(ActionTime AS DATE) = CAST(GETDATE() AS DATE)", conn);
                NewItemsTb.Text = Convert.ToString(cmdNew.ExecuteScalar());

                SqlCommand cmdSort = new SqlCommand(
                    "SELECT COUNT(*) FROM ActionLog WHERE ActionType='SORT' AND CAST(ActionTime AS DATE) = CAST(GETDATE() AS DATE)", conn);
                SortedItemsTb.Text = Convert.ToString(cmdSort.ExecuteScalar());

                string sqlUrgent = @"
                    SELECT COUNT(*) 
                    FROM Lot l
                    LEFT JOIN LotPlacement lp ON l.Lot_id = lp.Lot_id
                    WHERE DATEADD(hour, l.ShelfLifeHours, CAST(l.ArrivalDate AS DATETIME)) < DATEADD(day, 3, GETDATE())";

                SqlCommand cmdUrgent = new SqlCommand(sqlUrgent, conn);
                UrgentItemsTb.Text = Convert.ToString(cmdUrgent.ExecuteScalar());
            }
        }

        private void GoProfile(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new Profile_Page());
        }

        private void GoInventory(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new Inventory_Page());
        }

        private void GoLogs(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new Logs_Page());
        }

        private void GoIncoming(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new Incoming_Page());
        }

        private void GoOutgoing(object sender, RoutedEventArgs e)
        {
        }

        private void GoWorkers(object sender, RoutedEventArgs e)
        {
            NavigationService?.Navigate(new Users_Page());
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            UserData.CurrentUser = null;
            NavigationService?.Navigate(new Auth_Page());
        }
    }
}