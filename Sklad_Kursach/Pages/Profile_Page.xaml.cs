using Microsoft.Win32;
using Sklad_Kursach.Class;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace Sklad_Kursach.Pages
{
    public partial class Profile_Page : Page
    {
        string connStr = ConfigurationManager.ConnectionStrings["Warehouse_DB_V3"].ConnectionString;

        public Profile_Page()
        {
            InitializeComponent();
            UserName.Text = UserData.CurrentUser.FirstName + " " + UserData.CurrentUser.LastName;
            PostTb.Text = UserData.CurrentUser.Role;
            LoginTb.Text = UserData.CurrentUser.Login;
            LastVhod.Text = System.DateTime.Now.ToString();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateLastLogin();
            LoadUserPhoto();
        }

        private void UpdateLastLogin()
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                string query = "UPDATE Data_for_authorization SET LastVhod = @time WHERE Auth_id = @id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", UserData.CurrentUser.AuthId);
                cmd.Parameters.AddWithValue("@time", System.DateTime.Now);
                cmd.ExecuteNonQuery();
            }
        }

        private void LoadUserPhoto()
        {
            // Используем наш универсальный метод. Для Ellipse передаем 4-й параметр
            UserData.LoadAvatar(UserData.CurrentUser.AuthId, null, AvatarEmoji, UserAvatar);
        }

        private void ChangePhoto_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog op = new OpenFileDialog
            {
                Title = "Выберите фото",
                Filter = "Картинки|*.jpg;*.jpeg;*.png"
            };

            if (op.ShowDialog() == true)
            {
                byte[] imageBytes = File.ReadAllBytes(op.FileName);
                SavePhotoToDB(imageBytes);
                // Сразу обновляем вид на странице
                LoadUserPhoto();
            }
        }

        private void SavePhotoToDB(byte[] photo)
        {
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                string sql = "UPDATE Employee SET Photo = @photo WHERE Auth_id = @id";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@photo", photo);
                cmd.Parameters.AddWithValue("@id", UserData.CurrentUser.AuthId);
                cmd.ExecuteNonQuery();
            }
        }

        private void GoBack(object sender, RoutedEventArgs e)
        {
            NavigationService.GoBack();
        }
    }
}