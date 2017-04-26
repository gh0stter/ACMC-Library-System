using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Deployment.Application;
using System.Linq;
using System.Windows.Media;
using ACMC_Library_System.DbModels;
using NLog;
using ACMC_Library_System.Supports;

namespace ACMC_Library_System.UI
{
    /// <summary>
    /// Interaction logic for BootStrap.xaml
    /// </summary>
    public partial class BootStrap : INotifyPropertyChanged
    {
        #region Public Properties

        public double Percentage
        {
            get => _percentage;
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

        private static bool _autoBackupDb = true;
        private double _percentage;
        private static bool _appInitialized;
        private static bool _sqlConnected;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        private static Task<T> AsyncWrap<T>(T expression)
        {
            return Task.Run(() => expression);
        }

        public BootStrap()
        {
            InitializeComponent();
            DataContext = this;
            VisualHelper.Dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
            if (AppCheck())
            {
                InitData();
            }
            else
            {
                Close();
            }
        }

        public bool AppInitialized => _appInitialized;

        public bool AppPreparationSuccessfully => _appInitialized && _sqlConnected;

        public string AppVersion
        {
            get
            {
                string version;
                try
                {
                    version = ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString();
                    if (string.IsNullOrEmpty(Properties.Settings.Default.InstalledVersion))
                    {
                        Properties.Settings.Default.InstalledVersion = version;
                        Properties.Settings.Default.Save();
                    }
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
            _appInitialized = Properties.Settings.Default.Initialized;
            if (_appInitialized)
            {
                return true;
            }
            Logger.Info("App not initialized.");
            var sqlSetup = new SqlSetup();
            sqlSetup.ShowDialog();
            _appInitialized = sqlSetup.SetupSuccessfully;
            return _appInitialized;
        }

        private async void InitData()
        {
            var stopWatch = Stopwatch.StartNew();
            await Task.Run(async () =>
            {
                int taskSetp = 0;
                const double TaskCount = 10;
                if (string.IsNullOrWhiteSpace(Properties.Settings.Default.ConnectionString))
                {
                    Properties.Settings.Default.Reset();
                    Logger.Fatal("Empty connection string, user.config has been resetted.");
                    throw new ApplicationException("Fatal Error, unable to read connection string, configuration has been reseted.");
                }
                using (var context = new LibraryDb())
                {
                    if (_autoBackupDb)
                    {
                        try
                        {
                            string dbName = context.Database.Connection.Database;
                            string dbBackUpName = $"{dbName}_FullBackup_{DateTime.Now:yyyy_MM_dd}.bak";
                            string sqlCommand = $@"BACKUP DATABASE [{dbName}] TO DISK = N'{dbBackUpName}' WITH NOFORMAT, NOINIT, NAME = N'{dbName}-Full Database Backup', SKIP, NOREWIND, NOUNLOAD, STATS = 10";
                            context.Database.ExecuteSqlCommand(System.Data.Entity.TransactionalBehavior.DoNotEnsureTransaction, sqlCommand);
                        }
                        catch (Exception exception)
                        {
                            //suppress error but log it
                            Logger.Error(exception, "Unable to backup database.");
                        }
                    }
                    Percentage = ++taskSetp / TaskCount * 100;
                    Cache.ItemCategories = await AsyncWrap(context.item_category.ToList());
                    Percentage = ++taskSetp / TaskCount * 100;
                    Cache.ItemClasses = await AsyncWrap(context.item_class.ToList());
                    Percentage = ++taskSetp / TaskCount * 100;
                    Cache.ItemStatuses = await AsyncWrap(context.item_status.ToList());
                    Percentage = ++taskSetp / TaskCount * 100;
                    Cache.Members = await AsyncWrap(context.patron.ToList());
                    Percentage = ++taskSetp / TaskCount * 100;
                    Cache.Items = await AsyncWrap(context.item.ToList());
                    Percentage = ++taskSetp / TaskCount * 100;
                    Cache.ActionHistories = await AsyncWrap(context.action_history.OrderByDescending(i => i.id).Take(20).ToList());
                    Percentage = ++taskSetp / TaskCount * 100;
                    Parallel.ForEach(Cache.ActionHistories,
                                     action =>
                                     {
                                         action.MemberName = Cache.Members.FirstOrDefault(i => i.id == action.patronid)?.DisplayNameTitle;
                                         action.ItemName = Cache.Items.FirstOrDefault(i => i.id == action.itemid)?.title;
                                         action.ActionType = ((action_type.ActionTypeEnum)action.action_type).ToString();
                                     });
                    Percentage = ++taskSetp / TaskCount * 100;
                    Cache.ItemsShouldReturn = await AsyncWrap(context.item.Where(i => i.due_date < DateTime.Today).OrderByDescending(i => i.due_date).Take(20).ToList());
                    Percentage = ++taskSetp / TaskCount * 100;
                    Parallel.ForEach(Cache.ItemsShouldReturn,
                                     item =>
                                     {
                                         item.Borrower = Cache.Members.FirstOrDefault(i => i.id == item.patronid);
                                     });
                    Percentage = ++taskSetp / TaskCount * 100;
                    _sqlConnected = true;
                }
            });
            stopWatch.Stop();
#if DEBUG
            Debug.WriteLine($"Loading time: {stopWatch.Elapsed.TotalMilliseconds} ms.");
#endif
            Logger.Info($"Loading time: {stopWatch.Elapsed.TotalMilliseconds} ms.");
            Close();
        }
    }
}