using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Deployment.Application;
using System.Linq;
using System.Windows.Media;
using ACMC_Library_System.DbModels;
using NLog;
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
            await Task.Run(() =>
            {
                int taskSetp = 0;
                const double taskCount = 11;
                if (string.IsNullOrWhiteSpace(Properties.Settings.Default.ConnectionString))
                {
                    Properties.Settings.Default.Reset();
                    Logger.Fatal("Empty connection string, user.config has been resetted.");
                    throw new ApplicationException("Fatal Error, unable to read connection string, configuration has been reseted.");
                }
                using (var context = new LibraryDb())
                {
                    Percentage = ++taskSetp / taskCount * 100;
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
                    Percentage = ++taskSetp / taskCount * 100;
                    Cache.ItemCategories = context.item_category.ToList();
                    Percentage = ++taskSetp / taskCount * 100;
                    Cache.ItemClasses = context.item_class.ToList();
                    Percentage = ++taskSetp / taskCount * 100;
                    Cache.ItemStatuses = context.item_status.ToList();
                    Percentage = ++taskSetp / taskCount * 100;
                    Cache.Members = context.patron.ToList();
                    Percentage = ++taskSetp / taskCount * 100;
                    Cache.Items = context.item.ToList();
                    Percentage = ++taskSetp / taskCount * 100;
                    Cache.ActionHistories = context.action_history.OrderByDescending(i => i.id).Take(20).ToList();
                    Percentage = ++taskSetp / taskCount * 100;
                    foreach (var action in Cache.ActionHistories)
                    {
                        action.MemberName = Cache.Members.FirstOrDefault(i => i.id == action.patronid)?.DisplayNameTitle;
                        action.ItemName = Cache.Items.FirstOrDefault(i => i.id == action.itemid)?.title;
                        action.ActionType = action.action_type1.verb;
                    }
                    Percentage = ++taskSetp / taskCount * 100;
                    Cache.ItemsShouldReturn = context.item.Where(i => i.due_date < DateTime.Today).OrderByDescending(i => i.due_date).Take(20).ToList();
                    Percentage = ++taskSetp / taskCount * 100;
                    foreach (var item in Cache.ItemsShouldReturn)
                    {
                        item.Borrower = Cache.Members.FirstOrDefault(i => i.id == item.patronid);
                    }
                    Percentage = ++taskSetp / taskCount * 100;
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