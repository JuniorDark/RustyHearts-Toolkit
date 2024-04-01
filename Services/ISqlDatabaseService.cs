using System.Data;

namespace RHGMTool.Services
{
    public interface ISqlDatabaseService
    {
        IDbConnection OpenConnection(string databaseName);
        void EnsureConnectionClosed(IDbConnection connection);
        DataTable ExecuteDataQuery(string query, IDbConnection connection, IDbTransaction? transaction = null, params (string, object)[] parameters);
        void ExecuteQuery(string query, IDbConnection connection, Action<IDataReader> processResultsCallback, params (string, object)[] parameters);
        int ExecuteNonQuery(string query, IDbConnection connection, IDbTransaction? transaction = null, params (string, object?)[] parameters);
        object ExecuteScalar(string query, IDbConnection connection, params (string, object)[] parameters);
        object ExecuteProcedure(string procedureName, IDbConnection connection, IDbTransaction? transaction = null, params (string, object)[] parameters);
        DataTable ExecuteDataProcedure(string procedureName, IDbConnection connection, IDbTransaction? transaction = null, params (string, object)[] parameters);
    }
}
