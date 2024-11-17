using Microsoft.Data.SqlClient;
using RHToolkit.Models;
using System.Data;

namespace RHToolkit.Services
{
    /// <summary>
    /// Provides methods to interact with a SQL database.
    /// </summary>
    public class SqlDatabaseService : ISqlDatabaseService
    {
        /// <summary>
        /// Opens a connection to the specified database.
        /// </summary>
        /// <param name="databaseName">The name of the database to connect to.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the opened SqlConnection.</returns>
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

        /// <summary>
        /// Ensures that the specified connection is closed.
        /// </summary>
        /// <param name="connection">The SqlConnection to close.</param>
        public void EnsureConnectionClosed(SqlConnection connection)
        {
            if (connection.State != ConnectionState.Closed)
            {
                connection.Close();
            }
        }

        /// <summary>
        /// Tests the database connection.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation. The task result contains a tuple indicating success and an error message if any.</returns>
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

        /// <summary>
        /// Executes a scalar query.
        /// </summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="connection">The SqlConnection to use.</param>
        /// <param name="parameters">The parameters for the query.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the result of the query.</returns>
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

        /// <summary>
        /// Executes a data query and returns the result as a DataTable.
        /// </summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="connection">The SqlConnection to use.</param>
        /// <param name="transaction">The SqlTransaction to use, if any.</param>
        /// <param name="parameters">The parameters for the query.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the result of the query as a DataTable.</returns>
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

        /// <summary>
        /// Executes a query and processes the results using the specified callback.
        /// </summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="connection">The SqlConnection to use.</param>
        /// <param name="processResultsCallback">The callback to process the results.</param>
        /// <param name="parameters">The parameters for the query.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task ExecuteQueryAsync(string query, SqlConnection connection, Action<IDataReader> processResultsCallback, params (string, object)[] parameters)
        {
            using SqlCommand command = connection.CreateCommand();
            command.CommandText = query;
            AddParametersToCommand(command, parameters);

            using IDataReader reader = await command.ExecuteReaderAsync();
            processResultsCallback?.Invoke(reader);
        }

        /// <summary>
        /// Executes a non-query and returns the number of affected rows.
        /// </summary>
        /// <param name="query">The SQL query to execute.</param>
        /// <param name="connection">The SqlConnection to use.</param>
        /// <param name="transaction">The SqlTransaction to use, if any.</param>
        /// <param name="parameters">The parameters for the query.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the number of affected rows.</returns>
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

        /// <summary>
        /// Executes a stored procedure and returns the result.
        /// </summary>
        /// <param name="procedureName">The name of the stored procedure to execute.</param>
        /// <param name="connection">The SqlConnection to use.</param>
        /// <param name="transaction">The SqlTransaction to use, if any.</param>
        /// <param name="parameters">The parameters for the stored procedure.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the result of the stored procedure.</returns>
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

        /// <summary>
        /// Executes a stored procedure and returns the result as a DataTable.
        /// </summary>
        /// <param name="procedureName">The name of the stored procedure to execute.</param>
        /// <param name="connection">The SqlConnection to use.</param>
        /// <param name="transaction">The SqlTransaction to use, if any.</param>
        /// <param name="parameters">The parameters for the stored procedure.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the result of the stored procedure as a DataTable.</returns>
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