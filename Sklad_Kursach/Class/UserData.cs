using System;
using System.Configuration;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Sklad_Kursach.Class
{
    public class UserData
    {
        public static UserData CurrentUser { get; set; }

        public int AuthId { get; set; }
        public int EmployeeId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Role { get; set; }
        public string LastLogin { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }

        private string connStr = ConfigurationManager.ConnectionStrings["Warehouse_DB_V3"].ConnectionString;

        public UserData GetUser(string login, string password)
        {
            CurrentUser = null;
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                string query = @"
                    SELECT 
                        A.Auth_id, A.Login, A.Password, A.LastVhod,
                        E.Employee_id,
                        B.First_name, B.Last_name,
                        P.Post_Name
                    FROM Data_for_authorization A
                    JOIN Employee E ON A.Auth_id = E.Auth_id
                    JOIN FIO B ON E.FIO_id = B.FIO_id
                    JOIN Post P ON E.Post_id = P.Post_id
                    WHERE A.Login = @login AND A.Password = @pass";

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@login", login);
                cmd.Parameters.AddWithValue("@pass", password);

                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        CurrentUser = new UserData
                        {
                            AuthId = Convert.ToInt32(reader["Auth_id"]),
                            EmployeeId = Convert.ToInt32(reader["Employee_id"]),
                            Login = reader["Login"].ToString(),
                            Password = reader["Password"].ToString(),
                            LastLogin = reader["LastVhod"] != DBNull.Value ? reader["LastVhod"].ToString() : "Не входил",
                            FirstName = reader["First_name"].ToString(),
                            LastName = reader["Last_name"].ToString(),
                            Role = reader["Post_Name"].ToString()
                        };
                    }
                    return CurrentUser;
                }
            }
        }

        // Универсальный метод загрузки аватарки
        public static void LoadAvatar(int authId, Border borderContainer, TextBlock emojiBlock, Shape shapeContainer = null)
        {
            string connStr = ConfigurationManager.ConnectionStrings["Warehouse_DB_V3"].ConnectionString;
            byte[] photoBytes = null;

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand("SELECT Photo FROM Employee WHERE Auth_id = @id", conn);
                cmd.Parameters.AddWithValue("@id", authId);
                object result = cmd.ExecuteScalar();

                if (result != null && result != DBNull.Value)
                {
                    photoBytes = (byte[])result;
                }
            }

            if (photoBytes != null && photoBytes.Length > 0)
            {
                // Если фото есть
                using (var ms = new MemoryStream(photoBytes))
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = ms;
                    image.EndInit();

                    ImageBrush brush = new ImageBrush { ImageSource = image, Stretch = Stretch.UniformToFill };

                    // Скрываем смайлик
                    if (emojiBlock != null) emojiBlock.Visibility = Visibility.Collapsed;

                    // Устанавливаем фон
                    if (borderContainer != null) borderContainer.Background = brush;
                    if (shapeContainer != null) shapeContainer.Fill = brush;
                }
            }
            else
            {
                // Если фото нет - возвращаем дефолт
                if (emojiBlock != null) emojiBlock.Visibility = Visibility.Visible;

                var defaultBrush = (SolidColorBrush)new BrushConverter().ConvertFrom("#1565C0"); // Синий
                var lightBrush = new SolidColorBrush(Color.FromRgb(227, 242, 253)); // Светло-голубой (#E3F2FD)

                if (borderContainer != null) borderContainer.Background = defaultBrush;
                if (shapeContainer != null) shapeContainer.Fill = lightBrush;
            }
        }
    }
}