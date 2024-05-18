using RHToolkit.Models.MessageBox;

namespace RHToolkit.Models.Database
{
    public class SqlCredentialValidator
    {
        public static bool ValidateCredentials()
        {
            if (string.IsNullOrWhiteSpace(SqlCredentials.SQLServer) ||
                string.IsNullOrWhiteSpace(SqlCredentials.SQLUser) ||
                string.IsNullOrWhiteSpace(SqlCredentials.SQLPwd))
            {
                RHMessageBox.ShowOKMessage("SQL server credentials not set.\nSet the credentials in the Settings page.", "Empty SQL Server Credentials");
                return false;
            }

            return true;
        }
    }
}
