﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Configuration;
using System.Deployment.Application;
using System.Linq;
using ACMC_Library_System.DbModels;
using NLog;
using ACMC_Library_System.Entities;
using ACMC_Library_System.Supports;

namespace ACMC_Library_System.UI
{
    /// <summary>
    ///     Interaction logic for BootStrap.xaml
    /// </summary>
    public partial class BootStrap : Window, INotifyPropertyChanged
    {
        #region Public Properties

        public double Percentage
        {
            get { return _percentage; }
            set
            {
                if (Math.Abs(value - _percentage) < 0.0001)
                {
                    return;
                }
                _percentage = value;
                OnPropertyChanged("Percentage");
            }
        }

        #endregion

        #region private properties

        private static bool _autoUpdate = true;
        private static bool _autoBackupDb = true;
        private double _percentage;
        private static bool _appInitialized;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        public BootStrap()
        {
            InitializeComponent();
            DataContext = this;
            if (AppCheck())
            {
                InitData();
            }
            else
            {
                Close();
            }
        }

        public bool UserCancleSetup => !_appInitialized;

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

        private static bool AppCheck()
        {
            AppConfigCheck();
            if (_appInitialized)
            {
                return true;
            }
            Logger.Info("App not initialized.");
            var sqlSetup = new SqlSetup();
            sqlSetup.ShowDialog();
            return _appInitialized;
        }

        private async void InitData()
        {
            var stopWatch = Stopwatch.StartNew();
            await Task.Run(() =>
            {
                int taskSetp = 0;
                const double taskCount = 12;
                using (var context = new LibraryDb())
                {
                    Percentage = ++taskSetp / taskCount * 100;
                    if (_autoUpdate)
                    {
                        //todo run auto update
                    }
                    Percentage = ++taskSetp / taskCount * 100;
                    if (_autoBackupDb)
                    {
                        string dbName = context.Database.Connection.Database;
                        string dbBackUpName = $"{dbName}_FullBackup_{DateTime.Now:yyyy_MM_dd}.bak";
                        string sqlCommand = $@"BACKUP DATABASE [{dbName}] TO DISK = N'{dbBackUpName}' WITH NOFORMAT, NOINIT, NAME = N'{dbName}-Full Database Backup', SKIP, NOREWIND, NOUNLOAD, STATS = 10";
                        context.Database.ExecuteSqlCommand(System.Data.Entity.TransactionalBehavior.DoNotEnsureTransaction, sqlCommand);
                    }
                    Percentage = ++taskSetp / taskCount * 100;
                    Cache.ItemCategories = context.item_category.ToList();
                    Percentage = ++taskSetp / taskCount * 100;
                    Cache.ItemClasses = context.item_class.ToList();
                    Percentage = ++taskSetp / taskCount * 100;
                    Cache.ItemStatuses = context.item_status.ToList();
                    Percentage = ++taskSetp / taskCount * 100;
                    Cache.Users = context.patron.ToList();
                    Percentage = ++taskSetp / taskCount * 100;
                    Cache.Items = context.item.ToList();
                    Percentage = ++taskSetp / taskCount * 100;
                    Cache.ActionHistories = context.action_history.OrderByDescending(i => i.id).Take(20).ToList();
                    Percentage = ++taskSetp / taskCount * 100;
                    foreach (var action in Cache.ActionHistories)
                    {
                        action.UserName = Cache.Users.FirstOrDefault(i => i.id == action.patronid)?.DisplayNameTitle;
                        action.ItemName = Cache.Items.FirstOrDefault(i => i.id == action.itemid)?.title;
                        action.ActionType = action.action_type1.verb;
                    }
                    Percentage = ++taskSetp / taskCount * 100;
                    Cache.ItemsShouldReturn = context.item.Where(i => i.due_date < DateTime.Today).OrderByDescending(i => i.due_date).Take(20).ToList();
                    Percentage = ++taskSetp / taskCount * 100;
                    foreach (var item in Cache.ItemsShouldReturn)
                    {
                        item.Borrower = Cache.Users.FirstOrDefault(i => i.id == item.patronid);
                    }
                    Percentage = ++taskSetp / taskCount * 100;
                }
            });
            stopWatch.Stop();
#if DEBUG
            Debug.WriteLine($"Loading time: {stopWatch.Elapsed.TotalMilliseconds} ms.");
#endif
            Logger.Info($"Loading time: {stopWatch.Elapsed.TotalMilliseconds} ms.");
            Close();
        }

        private static void AppConfigCheck()
        {
            try
            {
                var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var settings = configFile.AppSettings.Settings;

                foreach (var kvp in AppSettings.AppControlKeys)
                {
                    if (settings[kvp.Key] == null)
                    {
                        settings.Add(kvp.Key, kvp.Value);
                    }
                    if (kvp.Key == AppSettings.AppInitialized)
                    {
                        bool.TryParse(settings[kvp.Key].Value, out _appInitialized);
                    }
                    if (kvp.Key == AppSettings.AutoUpdate)
                    {
                        bool.TryParse(settings[kvp.Key].Value, out _autoUpdate);
                    }
                    if (kvp.Key == AppSettings.AutoBackupDb)
                    {
                        bool.TryParse(settings[kvp.Key].Value, out _autoBackupDb);
                    }
                }
                configFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
            }
            catch (ConfigurationErrorsException exception)
            {
                Logger.Error(exception, "Unable to write settings to config file.");
                throw;
            }
        }

        private void Window_Closed(object sender, System.EventArgs e)
        {
            if (!_appInitialized)
            {
                Application.Current.Shutdown();
            }
        }
    }
}