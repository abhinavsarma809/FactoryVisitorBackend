using MySql.Data.MySqlClient;

namespace FactoryVisitorApp.Data
{
    public class DBHelper
    {
        private static string connectionString = "Server=localhost;Database=factory_visitor_db;Uid=root;Pwd=Abhisarma@2003;";

        public static MySqlConnection GetConnection()
        {
            return new MySqlConnection(connectionString);
        }
    }
}
