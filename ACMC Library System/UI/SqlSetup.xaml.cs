﻿using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ACMC_Library_System.Entities;
using ACMC_Library_System.Supports;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using NLog;

namespace ACMC_Library_System.UI
{
    /// <summary>
    /// SqlSetting.xaml 的交互逻辑
    /// </summary>
    public partial class SqlSetup : MetroWindow, INotifyPropertyChanged
    {
        private static string _sqlServer = "(localhost)";
        private static string _userName = string.Empty;
        private static string _catalog = "library";
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
            get { return _userName; }
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

        #endregion

        public bool SetupSuccessfully = false;

        public SqlSetup()
        {
            InitializeComponent();
            DataContext = this;
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
#if DEBUG
            var connectionInfo = new SqlConnectionInfo
            {
                SqlServer = @"192.168.80.141\MSSQLSERVER2014",
                IntegratedSecurity = false,
                UserId = "sa",
                Password = "lyzP@ssword1",
                Catalog = "library"
            };
            //var connectionInfo = new SqlConnectionInfo
            //{
            //    SqlServer = _sqlServer,
            //    IntegratedSecurity = CbAuthType.SelectedIndex == 0,
            //    UserId = _userName,
            //    Password = TbPassword.Password,
            //    Catalog = _catalog
            //};
#else
            var connectionInfo = new SqlConnectionInfo
            {
                SqlServer = _sqlServer,
                IntegratedSecurity = CbAuthType.SelectedIndex == 0,
                UserId = _userName,
                Password = TbPassword.Password,
                Catalog = _catalog
            };
#endif
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
#if DEBUG
                var connectionInfo = new SqlConnectionInfo
                {
                    SqlServer = @"192.168.80.141\MSSQLSERVER2014",
                    IntegratedSecurity = false,
                    UserId = "sa",
                    Password = "lyzP@ssword1",
                    Catalog = "library"
                };
                //var connectionInfo = new SqlConnectionInfo
                //{
                //    SqlServer = _sqlServer,
                //    IntegratedSecurity = CbAuthType.SelectedIndex == 0,
                //    UserId = _userName,
                //    Password = TbPassword.Password,
                //    Catalog = _catalog
                //};
#else
                var connectionInfo = new SqlConnectionInfo
                {
                    SqlServer = _sqlServer,
                    IntegratedSecurity = CbAuthType.SelectedIndex == 0,
                    UserId = _userName,
                    Password = TbPassword.Password,
                    Catalog = _catalog
                };
#endif
                var sqlHelper = new SqlServerHelper(connectionInfo);
                if (await sqlHelper.TestSqlConnection())
                {
                    var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    var settings = configFile.AppSettings.Settings;

                    foreach (var kvp in AppSettings.AppControlKeys)
                    {
                        if (settings[kvp.Key] == null)
                        {
                            settings.Add(kvp.Key, kvp.Value);
                        }
                        switch (kvp.Key)
                        {
                            case AppSettings.AppInitialized:
                                settings[kvp.Key].Value = "True";
                                break;
                            case AppSettings.SqlServer:
                                settings[kvp.Key].Value = SqlServer;
                                break;
                            case AppSettings.AuthType:
                                settings[kvp.Key].Value = ((KeyValuePair<int, string>)CbAuthType.SelectedItem).Key.ToString();
                                break;
                            case AppSettings.User:
                                settings[kvp.Key].Value = UserName;
                                break;
                            case AppSettings.Password:
                                settings[kvp.Key].Value = Encryption.Encrypt(TbPassword.Password, AppSettings.EncryptKey);
                                break;
                            case AppSettings.Catalog:
                                settings[kvp.Key].Value = Catalog;
                                break;
                        }
                    }
                    var connectionStringsSection = (ConnectionStringsSection)configFile.GetSection(configFile.ConnectionStrings.SectionInformation.Name);
                    connectionStringsSection.ConnectionStrings["Library"].ConnectionString = sqlHelper.GetConnectionString();
                    configFile.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
                    ConfigurationManager.RefreshSection(configFile.ConnectionStrings.SectionInformation.Name);
                    SetupSuccessfully = true;
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
