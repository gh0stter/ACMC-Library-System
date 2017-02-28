﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Deployment.Application;
using System.IO;
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
    /// AppSetting.xaml 的交互逻辑
    /// </summary>
    public partial class AppSettingWindow : MetroWindow
    {
        private static string _sqlServer = "(localhost)";
        private static string _userName = string.Empty;
        private static string _catalog = "library";
        private static bool _autoUpdate = true;
        private static bool _autoBackupDb = true;
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

        public bool AutoUpdate
        {
            get { return _autoUpdate; }
            set
            {
                _autoUpdate = value;
                OnPropertyChanged("AutoUpdate");
            }
        }

        public bool AutoBackDb
        {
            get { return _autoBackupDb; }
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

        #endregion

        public AppSettingWindow()
        {
            InitializeComponent();
            DataContext = this;
            // make window on top when user restore application focus from other place
            Owner = Application.Current.MainWindow;
#if DEBUG
            string exePath = Path.Combine(Environment.CurrentDirectory, "ACMC Library System.exe");
            var configFile = ConfigurationManager.OpenExeConfiguration(exePath);
#else
            var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
#endif
            var settings = configFile.AppSettings.Settings;
            foreach (var kvp in AppSettings.AppControlKeys)
            {
                if (settings[kvp.Key] == null)
                {
                    settings.Add(kvp.Key, kvp.Value);
                }
                switch (kvp.Key)
                {
                    case AppSettings.SqlServer:
                        SqlServer = settings[kvp.Key].Value;
                        break;
                    case AppSettings.AuthType:
                        int key;
                        int.TryParse(settings[kvp.Key].Value, out key);
                        CbAuthType.SelectedIndex = key;
                        break;
                    case AppSettings.User:
                        UserName = settings[kvp.Key].Value;
                        break;
                    case AppSettings.Password:
                        TbPassword.Password = Encryption.Decrypt(settings[kvp.Key].Value, AppSettings.EncryptKey);
                        break;
                    case AppSettings.Catalog:
                        Catalog = settings[kvp.Key].Value = _catalog;
                        break;
                    case AppSettings.AutoUpdate:
                        bool autoUpdate;
                        bool.TryParse(settings[kvp.Key].Value, out autoUpdate);
                        AutoUpdate = autoUpdate;
                        break;
                    case AppSettings.AutoBackupDb:
                        bool autoBackupDb;
                        bool.TryParse(settings[kvp.Key].Value, out autoBackupDb);
                        AutoBackDb = autoBackupDb;
                        break;
                }
            }
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
                    Cursor = Cursors.Arrow;
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
                                settings[kvp.Key].Value = ((KeyValuePair<int, string>) CbAuthType.SelectedItem).Key.ToString();
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
                            case AppSettings.AutoUpdate:
                                settings[kvp.Key].Value = AutoUpdate.ToString();
                                break;
                            case AppSettings.AutoBackupDb:
                                settings[kvp.Key].Value = AutoBackDb.ToString();
                                break;
                        }
                    }
                    var connectionStringsSection = (ConnectionStringsSection)configFile.GetSection(configFile.ConnectionStrings.SectionInformation.Name);
                    connectionStringsSection.ConnectionStrings["Library"].ConnectionString = sqlHelper.GetConnectionString();
                    configFile.Save(ConfigurationSaveMode.Modified);
                    ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
                    ConfigurationManager.RefreshSection(configFile.ConnectionStrings.SectionInformation.Name);
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