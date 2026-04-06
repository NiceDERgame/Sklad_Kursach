using Sklad_Kursach.Class;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;

namespace Sklad_Kursach.Pages
{
    public partial class Logs_Page : Page
    {
        public Logs_Page()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!UserData.EnsureAuthorized(this))
                    return;

                LoadLogs();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Ошибка загрузки журнала:\n" + ex.Message,
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void LoadLogs()
        {
            using (SqlConnection conn = new SqlConnection(UserData.GetConnectionString()))
            {
                conn.Open();

                string query = @"
                    SELECT 
                        L.ActionTime,
                        L.ActionType,
                        L.Details,
                        (F.Last_name + ' ' + LEFT(F.First_name, 1) + '.') AS EmployeeName
                    FROM dbo.ActionLog L
                    JOIN dbo.Employee E ON L.Employee_id = E.Employee_id
                    JOIN dbo.FIO F ON E.FIO_id = F.FIO_id
                    ORDER BY L.ActionTime DESC";

                SqlDataAdapter adapter = new SqlDataAdapter(query, conn);
                DataTable dt = new DataTable();
                adapter.Fill(dt);

                LogsGrid.ItemsSource = dt.DefaultView;
            }
        }

        private void GoBack_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService?.CanGoBack == true)
                NavigationService.GoBack();
        }
    }
}