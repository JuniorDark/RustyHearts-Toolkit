using System.Data;

namespace RHToolkit.Services
{
    public interface ISqlDatabaseService
    {
        void EnsureConnectionClosed(IDbConnection connection);
        DataTable ExecuteDataProcedure(string procedureName, IDbConnection connection, IDbTransaction? transaction = null, params (string, object)[] parameters);
        DataTable ExecuteDataQuery(string query, IDbConnection connection, IDbTransaction? transaction = null, params (string, object)[] parameters);
        int ExecuteNonQuery(string query, IDbConnection connection, IDbTransaction? transaction = null, params (string, object)[] parameters);
        object ExecuteProcedure(string procedureName, IDbConnection connection, IDbTransaction? transaction = null, params (string, object)[] parameters);
        void ExecuteQuery(string query, IDbConnection connection, Action<IDataReader> processResultsCallback, params (string, object)[] parameters);
        object ExecuteScalar(string query, IDbConnection connection, params (string, object)[] parameters);
        IDbConnection OpenConnection(string databaseName);
        Task<(bool success, string errorMessage)> TestDatabaseConnectionAsync();
    }
}