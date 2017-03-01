using System.Collections.Generic;

namespace ACMC_Library_System.Entities
{
    public class AppSettings
    {
        public const string EncryptKey = "ACMCLiby";
        public const string AppInitialized = "Initialized";
        public const string SqlServer = "SQLServer";
        public const string AuthType = "AuthType";
        public const string User = "User";
        public const string Password = "Password";
        public const string Catalog = "Catalog";
        public const string AutoBackupDb = "AutoBackupDb";
        public static readonly Dictionary<int, string> SqlAuthType = new Dictionary<int, string>()
        {
            { 0, "Windows Authentication"},
            { 1, "SQL Authentication" }
        }; 
        public static readonly Dictionary<string, string> AppControlKeys = new Dictionary<string, string>
        {
            { AppInitialized, "False"},
            { SqlServer, "(localhost)"},
            { AuthType,"0" },
            { User, ""},
            { Password, ""},
            { Catalog, ""},
            { AutoBackupDb, "True" }
        };
    }
}
