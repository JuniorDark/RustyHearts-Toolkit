using RHGMTool.Models;
using System.Data.SqlClient;
using System.Windows;

namespace RHGMTool.Services
{
    public class SqlDatabaseConnection
    {
        public static SqlConnection OpenSqlConnection(string databaseName)
        {
            try
            {
                string connectionString = $"Data Source={SqlCredentials.SQLServer};Initial Catalog={databaseName};User Id={SqlCredentials.SQLUser};Password={SqlCredentials.SQLPwd};";
                SqlConnection connection = new(connectionString);
                connection.Open();
                return connection;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening sql database connection: {ex.Message}", "Error");
                throw;
            }
        }

        public static async Task<(bool success, string errorMessage)> TestDatabaseConnectionAsync()
        {
            string connectionString = $"Data Source={SqlCredentials.SQLServer};User ID={SqlCredentials.SQLUser};Password={SqlCredentials.SQLPwd};";

            try
            {
                return await Task.Run(() =>
                {
                    using SqlConnection connection = new(connectionString);

                    try
                    {
                        connection.Open();
                        return (true, "");
                    }
                    catch (SqlException ex)
                    {
                        return (false, ex.Message);
                    }
                });
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

    }

}
