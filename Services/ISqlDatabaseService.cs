using Microsoft.Data.SqlClient;
using System.Data;

namespace RHToolkit.Services
{
    public interface ISqlDatabaseService
    {
        void EnsureConnectionClosed(SqlConnection connection);
        Task<DataTable> ExecuteDataProcedureAsync(string procedureName, SqlConnection connection, SqlTransaction? transaction = null, params (string, object)[] parameters);
        Task<DataTable> ExecuteDataQueryAsync(string query, SqlConnection connection, SqlTransaction? transaction = null, params (string, object)[] parameters);
        Task<int> ExecuteNonQueryAsync(string query, SqlConnection connection, SqlTransaction? transaction = null, params (string, object)[] parameters);
        Task<object> ExecuteProcedureAsync(string procedureName, SqlConnection connection, SqlTransaction? transaction = null, params (string, object)[] parameters);
        Task ExecuteQueryAsync(string query, SqlConnection connection, Action<IDataReader> processResultsCallback, params (string, object)[] parameters);
        Task<object?> ExecuteScalarAsync(string query, SqlConnection connection, params (string, object)[] parameters);
        Task<SqlConnection> OpenConnectionAsync(string databaseName);
        Task<(bool success, string errorMessage)> TestDatabaseConnectionAsync();
    }
}