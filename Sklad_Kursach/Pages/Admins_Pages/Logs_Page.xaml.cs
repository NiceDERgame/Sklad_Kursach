using Sklad_Kursach.Class;
using System.Configuration;
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
            LoadLogs();
        }

        private void LoadLogs()
        {
            string connStr = ConfigurationManager.ConnectionStrings["Warehouse_DB_V3"].ConnectionString;

            // Запрос соединяет логи с таблицами сотрудников и ФИО, 
            // чтобы получить человеко-читаемое имя.
            string query = @"
                SELECT 
                    L.ActionTime,
                    L.ActionType,
                    L.Details,
                    (F.Last_name + ' ' + LEFT(F.First_name, 1) + '.') AS EmployeeName
                FROM dbo.ActionLog L
                JOIN dbo.Employee E ON L.Employee_id = E.Employee_id
                JOIN dbo.FIO F ON E.FIO_id = F.FIO_id
                ORDER BY L.ActionTime DESC"; // Сортируем: новые сверху

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    conn.Open();
                    SqlDataAdapter adapter = new SqlDataAdapter(query, conn);
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    LogsGrid.ItemsSource = dt.DefaultView;
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show("Ошибка загрузки логов: " + ex.Message);
            }
        }

        private void GoBack_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
        }
    }
}