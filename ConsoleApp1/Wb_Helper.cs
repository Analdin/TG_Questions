using MySql.Data.MySqlClient;
using System.Data.SqlClient;
using System;
using System.Collections.Generic;

namespace WbHelperDB
{
    public class DbHelper
    {
        private const string Server = "37.140.192.191";
        private const string DatabaseName = "u1486803_newParserBD";
        private const string UserName = "u1486803_nuPR";
        private const string Password = "kP4mJ8xA0umM2bY6";

        public readonly MySqlConnection Connection;

        public DbHelper(MySqlConnection connection)
        {
            this.Connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }
        public DbHelper()
            : this(new MySqlConnection($"Server={Server}; database={DatabaseName}; UID={UserName}; password={Password};CharSet=UTF8;"))
        {
        }

        public void OpenConnection()
        {
            this.Connection.Open();
        }

        public void CloseConnection()
        {
            this.Connection.Close();
        }

        public bool TableExists(string tableName)
        {
            using (var cmd = new MySqlCommand())
            {
                cmd.Connection = Connection;
                cmd.CommandText = "SHOW TABLES LIKE @tableName";
                cmd.Parameters.AddWithValue("@tableName", tableName);

                using (var reader = cmd.ExecuteReader())
                {
                    return reader.HasRows;
                }
            }
        }

        public void CreateTableIfNotExists(string tableName)
        {
            if (!TableExists(tableName))
            {
                using (var cmd = new MySqlCommand())
                {
                    cmd.Connection = Connection;
                    if (tableName == "ParsedLinks")
                    {
                        cmd.CommandText = $"CREATE TABLE {tableName} (id INT AUTO_INCREMENT, link VARCHAR(255), PRIMARY KEY (id))";
                    }
                    if (tableName == "NewsTable")
                    {
                        cmd.CommandText = $"CREATE TABLE {tableName} (id INT AUTO_INCREMENT, titleName VARCHAR(255), bodyName TEXT, linkName VARCHAR(255), picLink VARCHAR(255), PRIMARY KEY (id))";
                    }
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public void InsertLink(string link)
        {
            using (var cmd = new MySqlCommand())
            {
                cmd.Connection = Connection;
                cmd.CommandText = "INSERT INTO ParsedLinks (link) VALUES (@link)";
                cmd.Parameters.AddWithValue("@link", link);
                cmd.ExecuteNonQuery();
            }
        }

        public void InsertLink2(string title, string body)
        {
            using (var cmd = new MySqlCommand())
            {
                cmd.Connection = Connection;
                cmd.CommandText = "INSERT INTO NewsTable (titleName, bodyName) VALUES (@title, @body)";
                cmd.Parameters.AddWithValue("@title", title);
                cmd.Parameters.AddWithValue("@body", body);
                cmd.ExecuteNonQuery();
            }
        }

        // For Insert, Update or Delete queries
        public void ExecuteNonQuery(string query)
        {
            using (var cmd = new MySqlCommand(query, this.Connection))
            {
                cmd.ExecuteNonQuery();
            }
        }

        public int ExecuteSimpleQueryAsInt(string query)
        {
            return int.Parse(this.ExecuteSimpleQueryAsString(query));
        }

        public string ExecuteSimpleQueryAsString(string query)
        {
            return this.ExecuteSimpleQuery(query).ToString();
        }

        private object ExecuteSimpleQuery(string query)
        {
            using (var cmd = new MySqlCommand(query, this.Connection))
            {
                cmd.ExecuteNonQuery();

                var reader = cmd.ExecuteReader();

                if (!reader.Read())
                    throw new Exception("Incorrect command?");

                return reader.GetValue(0);
            }
        }

        public List<string> GetAllTitles()
        {
            List<string> titles = new List<string>();

            using (var cmd = new MySqlCommand())
            {

                cmd.Connection = Connection;
                cmd.CommandText = "SELECT titleName FROM NewsTable";

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string title = reader.GetString("titleName");
                        titles.Add(title);
                    }
                }
            }

            return titles;
        }
    }
}
