using Microsoft.Data.SqlClient;
using RHToolkit.Models;
using System.Data;

namespace RHToolkit.Services
{
    public class SqlDatabaseService : ISqlDatabaseService
    {
        public async Task<SqlConnection> OpenConnectionAsync(string databaseName)
        {
            string connectionString = $"Data Source={SqlCredentials.SQLServer};Initial Catalog={databaseName};User Id={SqlCredentials.SQLUser};Password={SqlCredentials.SQLPwd};Encrypt=false;";
            SqlConnection connection = new()
            {
                ConnectionString = connectionString
            };
            await connection.OpenAsync();

            if (!string.IsNullOrEmpty(databaseName))
            {
                connection.ChangeDatabase(databaseName);
            }

            return connection;
        }

        public void EnsureConnectionClosed(SqlConnection connection)
        {
            if (connection.State != ConnectionState.Closed)
            {
                connection.Close();
            }
        }

        public async Task<(bool success, string errorMessage)> TestDatabaseConnectionAsync()
        {
            string connectionString = $"Data Source={SqlCredentials.SQLServer};User ID={SqlCredentials.SQLUser};Password={SqlCredentials.SQLPwd};Encrypt=false;";

            try
            {
                using SqlConnection connection = new(connectionString);
                try
                {
                    await connection.OpenAsync();
                    return (true, "");
                }
                catch (SqlException ex)
                {
                    return (false, ex.Message);
                }
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task<object?> ExecuteScalarAsync(string query, SqlConnection connection, params (string, object)[] parameters)
        {
            return await ExecuteScalarInternalAsync(query, connection, parameters);
        }

        private static async Task<object?> ExecuteScalarInternalAsync(string query, SqlConnection connection, params (string, object)[] parameters)
        {
            if (connection.State != ConnectionState.Open)
            {
                await connection.OpenAsync();
            }

            using var command = connection.CreateCommand();
            command.CommandText = query;
            AddParametersToCommand(command, parameters);

            object? result = await command.ExecuteScalarAsync();
            return result ?? DBNull.Value;
        }

        public async Task<DataTable> ExecuteDataQueryAsync(string query, SqlConnection connection, SqlTransaction? transaction = null, params (string, object)[] parameters)
        {
            DataTable dataTable = new();

            using (SqlCommand command = connection.CreateCommand())
            {
                command.CommandText = query;
                AddParametersToCommand(command, parameters);

                if (transaction != null)
                {
                    command.Transaction = transaction;
                }

                using IDataReader reader = await command.ExecuteReaderAsync();
                dataTable.Load(reader);
            }

            return dataTable;
        }

        public async Task ExecuteQueryAsync(string query, SqlConnection connection, Action<IDataReader> processResultsCallback, params (string, object)[] parameters)
        {
            using SqlCommand command = connection.CreateCommand();
            command.CommandText = query;
            AddParametersToCommand(command, parameters);

            using IDataReader reader = await command.ExecuteReaderAsync();
            processResultsCallback?.Invoke(reader);
        }

        public async Task<int> ExecuteNonQueryAsync(string query, SqlConnection connection, SqlTransaction? transaction = null, params (string, object)[] parameters)
        {
            using SqlCommand command = connection.CreateCommand();
            command.CommandText = query;
            if (transaction != null)
            {
                command.Transaction = transaction;
            }
            AddParametersToCommand(command, parameters);

            int affectedRows = await command.ExecuteNonQueryAsync();
            return affectedRows;
        }

        private static void AddParametersToCommand(SqlCommand command, params (string, object)[] parameters)
        {
            foreach (var (paramName, paramValue) in parameters)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = paramName;
                parameter.Value = paramValue ?? DBNull.Value;
                command.Parameters.Add(parameter);
            }
        }

        public async Task<object> ExecuteProcedureAsync(string procedureName, SqlConnection connection, SqlTransaction? transaction = null, params (string, object)[] parameters)
        {
            using SqlCommand command = connection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = procedureName;
            if (transaction != null)
            {
                command.Transaction = transaction;
            }
            AddParametersToCommand(command, parameters);

            object? result = await command.ExecuteScalarAsync();
            return result ?? DBNull.Value;
        }

        public async Task<DataTable> ExecuteDataProcedureAsync(string procedureName, SqlConnection connection, SqlTransaction? transaction = null, params (string, object)[] parameters)
        {
            DataTable dataTable = new();

            using SqlCommand command = connection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = procedureName;
            if (transaction != null)
            {
                command.Transaction = transaction;
            }
            AddParametersToCommand(command, parameters);

            using var adapter = new SqlDataAdapter(command);
            await Task.Run(() => adapter.Fill(dataTable));

            return dataTable;
        }
    }
}
