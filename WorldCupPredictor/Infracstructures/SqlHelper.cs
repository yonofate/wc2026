using System.Configuration;
using System.Data.SqlClient;

namespace Sendo.FileTransfer.Infracstructure
{
    public static class SqlHelper
    {
        public static SqlConnection OpenConnection()
        {
            var connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"];
            var connection = new SqlConnection(connectionString.ToString());
            connection.Open();
            return connection;
        }        
    }
}