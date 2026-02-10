using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sklad_Kursach.Class
{
    public class UserData
    {

        public static UserData CurrentUser { get; set; }
        public int AuthId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Role { get; set; }      // Должность (Post_Name)
        public string LastLogin { get; set; } // LastVhod
        public string Login { get; set; }
        public string Password { get; set; }


        private string connStr = ConfigurationManager.ConnectionStrings["Warehouse_DB_V3"].ConnectionString; // строка подключения из апишки

        public UserData GetUser(string login, string password)
        {
            CurrentUser = null; // Сбрасываем старого пользователя перед поиском нового

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                string query = @"
                    SELECT 
                        A.Auth_id,
                        A.Login,
                        A.Password,
                        A.LastVhod,
                        B.First_name,
                        B.Last_name,
                        P.Post_Name
                    FROM Data_for_authorization A
                    JOIN Employee E ON A.Auth_id = E.Auth_id
                    JOIN FIO B ON E.FIO_id = B.FIO_id
                    JOIN Post P ON E.Post_id = P.Post_id
                    WHERE A.Login = @login AND A.Password = @pass"; // sql запрос

                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@login", login); // ищем по логину и паролю юзера
                cmd.Parameters.AddWithValue("@pass", password);

                conn.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        CurrentUser = new UserData
                        {
                            AuthId = Convert.ToInt32(reader["Auth_id"]),
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
    }
}
