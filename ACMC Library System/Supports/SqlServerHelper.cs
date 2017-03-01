using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using NLog;

namespace ACMC_Library_System.Supports
{
    internal class SqlConnectionInfo
    {
        public string SqlServer { get; set; }
        public bool IntegratedSecurity { get; set; }
        public string Catalog { get; set; }
        public string UserId { get; set; }
        public string Password { get; set; }
    }

    internal class SqlServerHelper
    {
        private static string _connectionString;
        private const int ConnectionTimeout = 20;
        private static SqlConnectionInfo _connectionInfo;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        

        public SqlServerHelper(SqlConnectionInfo info)
        {
            _connectionInfo = info;
        }

        public async Task<bool> TestSqlConnection()
        {
            bool result = false;
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = _connectionInfo.SqlServer,
                IntegratedSecurity = _connectionInfo.IntegratedSecurity,
                InitialCatalog = _connectionInfo.Catalog,
                UserID = _connectionInfo.UserId,
                Password = _connectionInfo.Password,
                MultipleActiveResultSets = true,
                PersistSecurityInfo = true,
                ConnectTimeout = ConnectionTimeout,
                ApplicationName = "EntityFramework"
            };
            await Task.Run(() =>
            {
                using (var connection = new SqlConnection(builder.ConnectionString))
                {
                    try
                    {
                        connection.Open();
                        _connectionString = builder.ConnectionString;
                        result = true;
                    }
                    catch (SqlException e)
                    {
                        Logger.Error(e, "Unable to connect sql server.");
                        result = false;
                    }
                }
            });
            return result;
        }

        public string GetConnectionString()
        {
            if (string.IsNullOrEmpty(_connectionString))
            {
                var builder = new SqlConnectionStringBuilder
                {
                    DataSource = _connectionInfo.SqlServer,
                    IntegratedSecurity = _connectionInfo.IntegratedSecurity,
                    InitialCatalog = _connectionInfo.Catalog,
                    UserID = _connectionInfo.UserId,
                    Password = _connectionInfo.Password,
                    MultipleActiveResultSets = true,
                    PersistSecurityInfo = true,
                    ConnectTimeout = ConnectionTimeout,
                    ApplicationName = "EntityFramework"
                };
                _connectionString = builder.ConnectionString;
                return _connectionString;
            }
            else
            {
                return _connectionString;
            }
        }
    }
}
