using Microsoft.Data.SqlClient;
using RHToolkit.Models;
using System.Data;

namespace RHToolkit.Services
{
    public class SqlDatabaseService : ISqlDatabaseService
    {
        public IDbConnection OpenConnection(string databaseName)
        {
            string connectionString = $"Data Source={SqlCredentials.SQLServer};Initial Catalog={databaseName};User Id={SqlCredentials.SQLUser};Password={SqlCredentials.SQLPwd};Encrypt=false;";
            SqlConnection connection = new()
            {
                ConnectionString = connectionString
            };
            connection.Open();

            if (!string.IsNullOrEmpty(databaseName))
            {
                connection.ChangeDatabase(databaseName);
            }

            return connection;
        }

        public void EnsureConnectionClosed(IDbConnection connection)
        {
            if (connection.State != ConnectionState.Closed)
            {
                connection.Close();
            }
        }

        public object ExecuteScalar(string query, IDbConnection connection, params (string, object)[] parameters)
        {
            return ExecuteScalarInternal(query, connection, parameters);
        }

        private static object ExecuteScalarInternal(string query, IDbConnection connection, params (string, object)[] parameters)
        {
            if (connection.State != ConnectionState.Open)
            {
                connection.Open();
            }

            using var command = connection.CreateCommand();
            command.CommandText = query;
            AddParametersToCommand(command, parameters);

            object? result = command.ExecuteScalar();
            return result ?? DBNull.Value;
        }

        public DataTable ExecuteDataQuery(string query, IDbConnection connection, IDbTransaction? transaction = null, params (string, object)[] parameters)
        {
            DataTable dataTable = new();

            using (IDbCommand command = connection.CreateCommand())
            {
                command.CommandText = query;
                AddParametersToCommand(command, parameters);

                if (transaction != null)
                {
                    command.Transaction = transaction;
                }

                using IDataReader reader = command.ExecuteReader();
                dataTable.Load(reader);
            }

            return dataTable;
        }

        public void ExecuteQuery(string query, IDbConnection connection, Action<IDataReader> processResultsCallback, params (string, object)[] parameters)
        {
            using IDbCommand command = connection.CreateCommand();
            command.CommandText = query;
            AddParametersToCommand(command, parameters);

            using IDataReader reader = command.ExecuteReader();
            processResultsCallback?.Invoke(reader);
        }

        public int ExecuteNonQuery(string query, IDbConnection connection, IDbTransaction? transaction = null, params (string, object)[] parameters)
        {
            using IDbCommand command = connection.CreateCommand();
            command.CommandText = query;
            if (transaction != null)
            {
                command.Transaction = transaction;
            }
            AddParametersToCommand(command, parameters);

            int affectedRows = command.ExecuteNonQuery();
            return affectedRows;
        }

        private static void AddParametersToCommand(IDbCommand command, params (string, object)[] parameters)
        {
            foreach (var (paramName, paramValue) in parameters)
            {
                var parameter = command.CreateParameter();
                parameter.ParameterName = paramName;
                parameter.Value = paramValue ?? DBNull.Value;
                command.Parameters.Add(parameter);
            }
        }

        public object ExecuteProcedure(string procedureName, IDbConnection connection, IDbTransaction? transaction = null, params (string, object)[] parameters)
        {
            using IDbCommand command = connection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = procedureName;
            if (transaction != null)
            {
                command.Transaction = transaction;
            }
            AddParametersToCommand(command, parameters);

            object? result = command.ExecuteScalar();
            return result ?? DBNull.Value;
        }

        public DataTable ExecuteDataProcedure(string procedureName, IDbConnection connection, IDbTransaction? transaction = null, params (string, object)[] parameters)
        {
            DataTable dataTable = new();

            using IDbCommand command = connection.CreateCommand();
            command.CommandType = CommandType.StoredProcedure;
            command.CommandText = procedureName;
            if (transaction != null)
            {
                command.Transaction = transaction;
            }
            AddParametersToCommand(command, parameters);

            using var adapter = new SqlDataAdapter((SqlCommand)command);
            adapter.Fill(dataTable);

            return dataTable;
        }

    }

}
