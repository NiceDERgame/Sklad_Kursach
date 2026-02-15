using Sklad_Kursach.Class;
using System.Configuration;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace Sklad_Kursach.Pages
{
    public partial class UserHubPage : Page
    {
        public UserHubPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            WelcomeTb.Text = $"С возвращением, {UserData.CurrentUser.FirstName}";
            LoadUserStats();

            // Загрузка аватарки с подменой
            UserData.LoadAvatar(UserData.CurrentUser.AuthId, AvatarBorder, AvatarEmoji);
        }

        private void LoadUserStats()
        {
            string connStr = ConfigurationManager.ConnectionStrings["Warehouse_DB_V3"].ConnectionString;
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();

                string sqlNew = "SELECT COUNT(*) FROM Lot WHERE Lot_id NOT IN (SELECT Lot_id FROM LotPlacement)";
                SqlCommand cmdNew = new SqlCommand(sqlNew, conn);
                TotalNewItemsTb.Text = cmdNew.ExecuteScalar().ToString();

                string sqlIncoming = @"
                    SELECT COUNT(*) FROM ActionLog 
                    WHERE ActionType = 'INCOMING' 
                    AND Employee_id = @empId 
                    AND CAST(ActionTime AS DATE) = CAST(GETDATE() AS DATE)";
                SqlCommand cmdInc = new SqlCommand(sqlIncoming, conn);
                cmdInc.Parameters.AddWithValue("@empId", UserData.CurrentUser.EmployeeId);
                MyIncomingStats.Text = cmdInc.ExecuteScalar().ToString();

                string sqlSort = @"
                    SELECT COUNT(*) FROM ActionLog 
                    WHERE ActionType = 'SORT' 
                    AND Employee_id = @empId 
                    AND CAST(ActionTime AS DATE) = CAST(GETDATE() AS DATE)";
                SqlCommand cmdSort = new SqlCommand(sqlSort, conn);
                cmdSort.Parameters.AddWithValue("@empId", UserData.CurrentUser.EmployeeId);
                MySortStats.Text = cmdSort.ExecuteScalar().ToString();

                string sqlShip = @"
                    SELECT COUNT(*) FROM ActionLog 
                    WHERE ActionType = 'PICKING' 
                    AND Employee_id = @empId 
                    AND CAST(ActionTime AS DATE) = CAST(GETDATE() AS DATE)";
                SqlCommand cmdShip = new SqlCommand(sqlShip, conn);
                cmdShip.Parameters.AddWithValue("@empId", UserData.CurrentUser.EmployeeId);
                MyShipmentStats.Text = cmdShip.ExecuteScalar().ToString();

                string sqlUrgent = @"
                    SELECT COUNT(*) 
                    FROM Lot l
                    LEFT JOIN LotPlacement lp ON l.Lot_id = lp.Lot_id
                    WHERE DATEADD(hour, l.ShelfLifeHours, CAST(l.ArrivalDate AS DATETIME)) < DATEADD(day, 3, GETDATE())";
                SqlCommand cmdUrgent = new SqlCommand(sqlUrgent, conn);
                UrgentItemsTb.Text = cmdUrgent.ExecuteScalar().ToString();
            }
        }

        private void GoProfile(object sender, RoutedEventArgs e) => NavigationService.Navigate(new Profile_Page());
        private void GoInventory(object sender, RoutedEventArgs e) => NavigationService.Navigate(new Inventory_Page());
        private void GoIncoming(object sender, RoutedEventArgs e) => NavigationService.Navigate(new Incoming_Page());
        private void GoSorting(object sender, RoutedEventArgs e) => NavigationService.Navigate(new Sort_Page(0, ""));
        private void GoOutgoing(object sender, RoutedEventArgs e) { }
        private void GoUsers(object sender, RoutedEventArgs e) => NavigationService.Navigate(new Users_Page());
        private void Logout(object sender, RoutedEventArgs e) => NavigationService.Navigate(new Auth_Page());
    }
}