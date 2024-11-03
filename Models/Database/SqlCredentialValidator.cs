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
                RHMessageBoxHelper.ShowOKMessage(Resources.SQLServerCredentialsMessage, Resources.SQLServerCredentialsTitle);
                return false;
            }

            return true;
        }
    }
}
