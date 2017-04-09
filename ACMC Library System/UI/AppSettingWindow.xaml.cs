using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Deployment.Application;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ACMC_Library_System.Properties;
using ACMC_Library_System.Supports;
using DomainModels.DataModel;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using NLog;

namespace ACMC_Library_System.UI
{
    /// <summary>
    /// AppSetting.xaml 的交互逻辑
    /// </summary>
    public partial class AppSettingWindow : MetroWindow
    {
        private static string _sqlServer = "(localhost)";
        private static string _userName = string.Empty;
        private static string _catalog = "library";
        private static bool _autoBackupDb = true;
        private static readonly Settings Settings = Settings.Default;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        #region UI Data binding Relates

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string SqlServer
        {
            get
            {
                if (string.IsNullOrEmpty(_sqlServer) || _sqlServer == "(localhost)")
                {
                    return _sqlServer;
                }
                return _sqlServer;
            }
            set
            {
                _sqlServer = value;
                OnPropertyChanged("SqlServer");
            }
        }

        public Dictionary<int, string> SqlAuthType => AppSettings.SqlAuthType;

        public string UserName
        {
            get => _userName;
            set
            {
                _userName = value;
                OnPropertyChanged("UserName");
            }
        }

        public string Catalog
        {
            get
            {
                if (string.IsNullOrEmpty(_catalog) || _catalog == "library")
                {
                    return _catalog;
                }
                else
                {
                    return _catalog;
                }
            }
            set
            {
                _catalog = value;
                OnPropertyChanged("Catalog");
            }
        }

        public bool AutoBackDb
        {
            get => _autoBackupDb;
            set
            {
                _autoBackupDb = value;
                OnPropertyChanged("AutoBackDb");
            }
        }

        public string AppVersion
        {
            get
            {
                string version;
                try
                {
                    version = ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString();
                }
                catch (InvalidDeploymentException)
                {
                    version = "Unkown";
                }
                return version;
            }
        }

        public int MemberRenewPeriodInYear => BusinessRules.MemberRenewPeriodInYEar;

        public int ItemRenewPeriodInDay => BusinessRules.ItemRenewPeriodInDay;

        public int DefaultQuotaPerMember => BusinessRules.DefaultQuotaPerMember;

        public double FinesPerWeek => BusinessRules.FinesPerWeek;

        #endregion

        public AppSettingWindow()
        {
            InitializeComponent();
            DataContext = this;
            // make window on top when user restore application focus from other place
            Owner = Application.Current.MainWindow;
            Settings.Reload();
            SqlServer = Settings.SQLServer;
            CbAuthType.SelectedIndex = Settings.AuthType;
            UserName = Settings.User;
            TbPassword.Password = Encryption.Decrypt(Settings.Password, AppSettings.EncryptKey);
            Catalog = Settings.Catalog;
            AutoBackDb = Settings.AutoBackupDb;
        }

        private void SetReadOnly(bool setTo)
        {
            TbSqlServer.IsEnabled = setTo;
            CbAuthType.IsHitTestVisible = setTo;
            TbUser.IsEnabled = CbAuthType.SelectedIndex != 0 && setTo;
            TbPassword.IsEnabled = CbAuthType.SelectedIndex != 0 && setTo;
            TbCatalog.IsEnabled = setTo;
            BtnSave.IsEnabled = setTo;
            BtnTest.IsEnabled = setTo;
            BtnCancel.IsEnabled = setTo;
        }

        private void TbAuthType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TbUser == null || TbPassword == null)
            {
                return;
            }
            TbUser.IsEnabled = CbAuthType.SelectedIndex != 0;
            TbPassword.IsEnabled = CbAuthType.SelectedIndex != 0;
        }

        private async void BtnTest_Click(object sender, RoutedEventArgs e)
        {
            SetReadOnly(false);
            Cursor = Cursors.Wait;
            var connectionInfo = new SqlConnectionInfo
            {
                SqlServer = _sqlServer,
                IntegratedSecurity = CbAuthType.SelectedIndex == 0,
                UserId = _userName,
                Password = TbPassword.Password,
                Catalog = _catalog
            };
            var sqlHelper = new SqlServerHelper(connectionInfo);
            Logger.Info($"Current ConnectionString: {sqlHelper.GetConnectionString()}");
            if (await sqlHelper.TestSqlConnection())
            {
                SetReadOnly(true);
                Cursor = Cursors.Arrow;
                await this.ShowMessageAsync("Sucess", "Connection Sucess.");
            }
            else
            {
                SetReadOnly(true);
                Cursor = Cursors.Arrow;
                await this.ShowMessageAsync("Error", "Connection fail, please check your connection details.");
            }
        }

        private async void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SetReadOnly(false);
                Cursor = Cursors.Wait;
                var connectionInfo = new SqlConnectionInfo
                {
                    SqlServer = _sqlServer,
                    IntegratedSecurity = CbAuthType.SelectedIndex == 0,
                    UserId = _userName,
                    Password = TbPassword.Password,
                    Catalog = _catalog
                };
                var sqlHelper = new SqlServerHelper(connectionInfo);
                if (await sqlHelper.TestSqlConnection())
                {
                    Settings.Reload();
                    Settings.Initialized = true;
                    Settings.SQLServer = SqlServer;
                    Settings.AuthType = ((KeyValuePair<int, string>)CbAuthType.SelectedItem).Key;
                    Settings.User = UserName;
                    Settings.Password = Encryption.Encrypt(TbPassword.Password, AppSettings.EncryptKey);
                    Settings.Catalog = Catalog;
                    Settings.ConnectionString = sqlHelper.GetConnectionString();
                    Settings.AutoBackupDb = AutoBackDb;
                    Settings.Save();
                    Cursor = Cursors.Arrow;
                    Close();
                }
                else
                {
                    Cursor = Cursors.Arrow;
                    SetReadOnly(true);
                    await this.ShowMessageAsync("Error", "Connection fail, please check your connection details.");
                }
            }
            catch (ConfigurationErrorsException exception)
            {
                Logger.Error(exception, "Unable to write settings to config file.");
                throw;
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
