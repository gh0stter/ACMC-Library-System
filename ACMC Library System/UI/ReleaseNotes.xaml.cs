using System.ComponentModel;
using System.Windows;
using Octokit;

namespace ACMC_Library_System.UI
{
    /// <summary>
    /// ReleaseNotes.xaml 的交互逻辑
    /// </summary>
    public partial class ReleaseNotes: INotifyPropertyChanged
    {
        private string _latestVersion;
        private string _releaseDate;
        private string _releaseNote;

        public string LatestVersion
        {
            get => _latestVersion;
            set
            {
                _latestVersion = value;
                OnPropertyChanged("LastestVersion");
            }
        }

        public string ReleaseDate
        {
            get => _releaseDate;
            set
            {
                _releaseDate = value;
                OnPropertyChanged("ReleaseDate");
            }
        }
        public string ReleaseNote
        {
            get => _releaseNote;
            set
            {
                _releaseNote = value;
                OnPropertyChanged("ReleaseNote");
            }
        }

        #region UI Data binding Relates

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        public ReleaseNotes(Release latestRelease)
        {
            this.DataContext = this;
            LatestVersion = latestRelease.TagName;
            ReleaseDate = latestRelease.PublishedAt?.ToString("yyyy-MM-dd") ?? "N/A";
            ReleaseNote = latestRelease.Body.Replace("*", "●");

            InitializeComponent();
            //make window on top when user restore application focus from other place
            Owner = System.Windows.Application.Current.MainWindow;
        }
    }
}
