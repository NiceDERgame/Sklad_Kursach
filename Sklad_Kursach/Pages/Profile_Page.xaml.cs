using Sklad_Kursach.Class;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Sklad_Kursach.Pages
{
    /// <summary>
    /// Логика взаимодействия для Profile_Page.xaml
    /// </summary>
    public partial class Profile_Page : Page
    {
        public Profile_Page()
        {
            InitializeComponent();

            string nowTime = System.DateTime.Now.ToString();
            LastVhod.Text = nowTime;

            UserName.Text = UserData.CurrentUser.FirstName + " " + UserData.CurrentUser.LastName;
            PostTb.Text = UserData.CurrentUser.Role;
            LoginTb.Text = UserData.CurrentUser.Login;

            string connStr = ConfigurationManager.ConnectionStrings["Warehouse_DB_V3"].ConnectionString;

                using (SqlConnection conn = new SqlConnection(connStr)) // последний вход записываем
                {
                    string query2 = "UPDATE Data_for_authorization SET LastVhod = @LastVhod WHERE Auth_id = @id";

                    SqlCommand cmd2 = new SqlCommand(query2, conn);

                    cmd2.Parameters.AddWithValue("@id", UserData.CurrentUser.AuthId);

                    cmd2.Parameters.AddWithValue("@LastVhod", nowTime);

                    conn.Open();

                    cmd2.ExecuteNonQuery();
                } 
        }

        private void GoBack(object sender, RoutedEventArgs e)
        {
            this.NavigationService.GoBack();
        }
    }
}
