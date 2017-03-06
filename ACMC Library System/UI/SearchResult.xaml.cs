using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ACMC_Library_System.DbModels;
using DomainModels.ViewModel;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using NLog;

namespace ACMC_Library_System.UI
{
    public partial class SearchResultWindow : MetroWindow
    {
        private readonly List<SearchResult> _searchResult;
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        public SearchResultWindow(List<SearchResult> searchResult)
        {
            InitializeComponent();
            _searchResult = searchResult;
            //make window on top when user restore application focus from other place
            Owner = Application.Current.MainWindow;
        }

        //initialize data
        private void IniData(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Visibility != Visibility.Visible)
            {
                return;
            }
            LbxSearchResult.ItemsSource = _searchResult;
        }

        private async void LbSearchResult_DbClick(object sender, MouseButtonEventArgs e)
        {
            var selectedResult = LbxSearchResult.SelectedItem as SearchResult;
            if (selectedResult == null)
            {
                Logger.Error("Error on double click search result: SelectedItem cast to null value.");
                await this.ShowMessageAsync("Error", "Data Error, unable to process selected search result.");
            }
            else
            {
                var mainWindow = (from object window in Application.Current.Windows
                                  where window.GetType() == typeof(MainWindow)
                                  select window as MainWindow).FirstOrDefault();
                if (mainWindow == null)
                {
                    return;
                }
                switch (selectedResult.RecordType)
                {
                    case SearchResultTypes.Member:
                        mainWindow.TabMember.IsSelected = true;
                        mainWindow.DgMemberGrid.SelectedItem = mainWindow.DgMemberGrid.Items.OfType<patron>().FirstOrDefault(i => i.id == selectedResult.MemberId);
                        mainWindow.DgMemberGrid.UpdateLayout();
                        mainWindow.DgMemberGrid.ScrollIntoView(mainWindow.DgMemberGrid.SelectedItem);
                        Close();
                        break;
                    case SearchResultTypes.Item:
                        mainWindow.TabItem.IsSelected = true;
                        mainWindow.DgItemGrid.SelectedItem = mainWindow.DgItemGrid.Items.OfType<item>().FirstOrDefault(i => i.id == selectedResult.ItemId);
                        mainWindow.DgItemGrid.UpdateLayout();
                        mainWindow.DgItemGrid.ScrollIntoView(mainWindow.DgItemGrid.SelectedItem);
                        Close();
                        break;
                }
            }
        }
    }
}