using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Deployment.Application;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Transactions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ACMC_Library_System.DbModels;
using ACMC_Library_System.Supports;
using DomainModels.DataModel;
using DomainModels.ViewModel;
using EntityFramework.Extensions;
using MahApps.Metro.Controls.Dialogs;
using NLog;
using Octokit;
using Rectangle = System.Windows.Shapes.Rectangle;

namespace ACMC_Library_System.UI
{
    /// <inheritdoc />
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        #region Private Properties

        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly string _currentDirectoryPath = Directory.GetCurrentDirectory();
        private const int NotificationFadeTimeInSecond = 5;
        private const string NoPictureImgPath = @"\Resources\UI Icons\NoImg.png";
        private const string ItemDefaultImgPath = @"\Resources\UI Icons\ItemIcon.png";
        private const string ItemBookImgPath = @"\Resources\UI Icons\Book.png";
        private const string ItemCdImgPath = @"\Resources\UI Icons\CD.png";
        private const string ItemTapeImgPath = @"\Resources\UI Icons\Tape.png";

        private readonly ExecutionDataflowBlockOptions _tplOption = new ExecutionDataflowBlockOptions
        {
            BoundedCapacity = 1000,
            MaxDegreeOfParallelism = 8
        };
        private readonly MetroDialogSettings _binarySelectionDialogSettings = new MetroDialogSettings
        {
            AffirmativeButtonText = "Yes",
            NegativeButtonText = "No",
            DefaultButtonFocus = MessageDialogResult.Negative
        };

        private string _memberFilter = string.Empty;
        private patron _selectedMember = new patron();

        private string _itemFilter = string.Empty;
        private item _selectedItem = new item();

        private bool _isMemberEditingMode;
        private bool _isAddingNewMember;

        private bool _isItemEditingMode;
        private bool _isAddingNewItem;

        #endregion

        #region Public Properties

        public List<item_category> ItemCategories => Cache.ItemCategories;

        public List<item_class> ItemClasses => Cache.ItemClasses;

        public List<item_status> ItemStatuses => Cache.ItemStatuses;

        /// <summary>
        /// Getter, get Member list from database
        /// </summary>
        public List<patron> MemberList
        {
            get
            {
                return _memberFilter.Trim() == string.Empty
                    ? Cache.Members
                    : Cache.Members.Where(member => member.id.ToString().Contains(_memberFilter) ||
                                                member.barcode.IndexOf(_memberFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                member.firstnames_ch?.IndexOf(_memberFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                member.firstnames_en?.IndexOf(_memberFilter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            }
        }

        /// <summary>
        /// Getter, get item list from database
        /// </summary>
        public List<item> ItemList
        {
            get
            {
                return _itemFilter.Trim() == string.Empty
                    ? Cache.Items
                    : Cache.Items.Where(item => item.id.ToString().Contains(_itemFilter) ||
                                                item.barcode?.IndexOf(_itemFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                item.title?.IndexOf(_itemFilter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
            }
        }

        public patron SelectedMember
        {
            get => _selectedMember;
            set
            {
                if (value == null || AreSameMember(value, _selectedMember))
                {
                    return;
                }
                if (value.id == -1)
                {
                    _selectedMember = value;
                }
                else
                {
                    using (var context = new LibraryDb())
                    {
                        _selectedMember = context.patron.First(member => member.id == value.id);
                        _selectedMember.BorrowingItems = context.item.Where(item => item.patronid == _selectedMember.id).ToList();
                    }
                }
                OnPropertyChanged("SelectedMember");
            }
        }

        public bool IsMemberEditMode
        {
            get => _isMemberEditingMode;
            set
            {
                _isMemberEditingMode = value;
                OnPropertyChanged("IsMemberEditMode");
            }
        }

        public item SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (value == null || AreSameItem(value, _selectedItem))
                {
                    return;
                }
                if (value.id == -1)
                {
                    _selectedItem = value;
                }
                else
                {
                    using (var context = new LibraryDb())
                    {
                        _selectedItem = context.item.First(item => item.id == value.id);
                        _selectedItem.Borrower = context.patron.FirstOrDefault(member => member.id == _selectedItem.patronid);
                    }
                }
                OnPropertyChanged("SelectedItem");
            }
        }

        public bool IsItemEditMode
        {
            get => _isItemEditingMode;
            set
            {
                _isItemEditingMode = value;
                OnPropertyChanged("IsItemEditMode");
            }
        }

        public bool IsAddingNewItem
        {
            get => _isAddingNewItem;
            set
            {
                _isAddingNewItem = value;
                OnPropertyChanged("IsAddingNewItem");
            }
        }

        public List<action_history> ActionHistories
        {
            get
            {
                var actions = Cache.ActionHistories.OrderByDescending(i => i.id).Take(20).ToList();
                Parallel.ForEach(actions,
                                 action =>
                                 {
                                     action.MemberName = Cache.Members.FirstOrDefault(i => i.id == action.patronid)?.DisplayNameTitle;
                                     action.ItemName = Cache.Items.FirstOrDefault(i => i.id == action.itemid)?.title;
                                     action.ActionType = ((action_type.ActionTypeEnum)action.action_type).ToString();
                                 });
                return actions;
            }
        }

        public List<item> ItemsShouldReturn
        {
            get
            {
                var items = Cache.Items.Where(i => i.due_date < DateTime.Today).OrderByDescending(i => i.due_date).Take(20).ToList();
                Parallel.ForEach(items,
                                 item =>
                                 {
                                     item.Borrower = Cache.Members.FirstOrDefault(i => i.id == item.patronid);
                                 });
                return items;
            }
        }

        #endregion

        #region UI Data binding Relates

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Private Methods

        private string GetItemImgPath(int? itemClass)
        {
            switch (itemClass)
            {
                case 1:
                case 5:
                case 6:
                case 8:
                    return _currentDirectoryPath + ItemBookImgPath;
                case 2:
                case 3:
                case 4:
                    return _currentDirectoryPath + ItemCdImgPath;
                case 7:
                case 9:
                    return _currentDirectoryPath + ItemTapeImgPath;
                case null:
                    return _currentDirectoryPath + ItemDefaultImgPath;
                default:
                    return _currentDirectoryPath + ItemDefaultImgPath;
            }
        }

        private static byte[] GetImageBytes(string filePath)
        {
            var image = (Bitmap)System.Drawing.Image.FromFile(filePath);
            byte[] imageBytes;
            using (var memoryStream = new MemoryStream())
            {
                image.Save(memoryStream, image.RawFormat);
                imageBytes = memoryStream.ToArray();
            }
            return imageBytes;
        }

        private Task<MessageDialogResult> UnsavedDialog()
        {
            var dialogSettings = new MetroDialogSettings
            {
                AffirmativeButtonText = "Yes",
                NegativeButtonText = "No",
                DefaultButtonFocus = MessageDialogResult.Negative
            };
            return this.ShowMessageAsync("Unsaved Changes",
                                         "You have unsaved work! Are you sure you want to cancel it without saving?",
                                         MessageDialogStyle.AffirmativeAndNegative,
                                         dialogSettings);
        }

        private Task<MessageDialogResult> NavigateDialog(string msg)
        {
            var dialogSettings = new MetroDialogSettings
            {
                AffirmativeButtonText = "OK",
                NegativeButtonText = "Cancel",
                DefaultButtonFocus = MessageDialogResult.Affirmative
            };
            return this.ShowMessageAsync("Warning", msg, MessageDialogStyle.AffirmativeAndNegative, dialogSettings);
        }

        private static bool AreSameMember(patron source, patron target)
        {
            return source.DisplayNameCh == target.DisplayNameCh &&
                   source.DisplayNameEn == target.DisplayNameEn &&
                   source.BorrowingItems == target.BorrowingItems &&
                   source.barcode == target.barcode &&
                   source.picture.NullSequenceEqual(target.picture) &&
                   source.limit == target.limit &&
                   source.address == target.address &&
                   source.phone == target.phone &&
                   source.email == target.email &&
                   source.created == target.created &&
                   source.expiry == target.expiry;
        }

        private static bool AreSameItem(item source, item target)
        {
            return source.barcode == target.barcode &&
                   source.code == target.code &&
                   source.patronid == target.patronid &&
                   source.title == target.title &&
                   source.category == target.category &&
                   source.item_subclass == target.item_subclass &&
                   source.status == target.status &&
                   source.isbn == target.isbn &&
                   source.publisher == target.publisher &&
                   source.author == target.author &&
                   source.published_date == target.published_date &&
                   source.due_date == target.due_date &&
                   source.pages == target.pages &&
                   source.minutes == target.minutes &&
                   source.donator == target.donator &&
                   source.price == target.price &&
                   source.moreinfo == target.moreinfo;
        }

        private static bool IsValidMember(patron member)
        {
            return (!string.IsNullOrWhiteSpace(member.DisplayNameCh) || !string.IsNullOrWhiteSpace(member.DisplayNameEn)) && !string.IsNullOrWhiteSpace(member.barcode);
        }

        private static bool IsMemberChanged(patron member)
        {
            if (member.id == -1)
            {
                return true;
            }
            using (var context = new LibraryDb())
            {
                var memberInDb = context.patron.First(i => i.id == member.id);
                return !(memberInDb.DisplayNameCh == member.DisplayNameCh &&
                         memberInDb.DisplayNameEn == member.DisplayNameEn &&
                         memberInDb.barcode == member.barcode &&
                         memberInDb.picture.NullSequenceEqual(member.picture) &&
                         memberInDb.limit == member.limit &&
                         memberInDb.address == member.address &&
                         memberInDb.phone == member.phone &&
                         memberInDb.email == member.email &&
                         memberInDb.created == member.created &&
                         memberInDb.expiry == member.expiry);
            }
        }

        private static bool IsValidItem(item item)
        {
            return !string.IsNullOrWhiteSpace(item.barcode) &&
                   !string.IsNullOrWhiteSpace(item.code) &&
                   !string.IsNullOrEmpty(item.title) &&
                   item.category != null &&
                   item.item_subclass != null &&
                   item.status != null;
        }

        private static bool IsItemChanged(item item)
        {
            if (item.id == -1)
            {
                return true;
            }
            using (var context = new LibraryDb())
            {
                var itemInDb = context.item.First(i => i.id == item.id);
                return !(itemInDb.barcode == item.barcode &&
                         itemInDb.code == item.code &&
                         itemInDb.patronid == item.patronid &&
                         itemInDb.title == item.title &&
                         itemInDb.category == item.category &&
                         itemInDb.item_subclass == item.item_subclass &&
                         itemInDb.status == item.status &&
                         itemInDb.isbn == item.isbn &&
                         itemInDb.publisher == item.publisher &&
                         itemInDb.author == item.author &&
                         itemInDb.published_date == item.published_date &&
                         itemInDb.due_date == item.due_date &&
                         itemInDb.pages == item.pages &&
                         itemInDb.minutes == item.minutes &&
                         itemInDb.donator == item.donator &&
                         itemInDb.price == item.price &&
                         itemInDb.moreinfo == item.moreinfo);
            }
        }

        private async void AddActionHistory(LibraryDb context, int? memberId, int itemId, action_type.ActionTypeEnum actionType)
        {
            context.action_history.Add(new action_history
            {
                patronid = memberId,
                itemid = itemId,
                action_type = (int)actionType,
                action_datetime = DateTime.Now
            });
            await Cache.RefreshMainCache();
            await RefreshGridSource(DgActionHistory);
            await RefreshGridSource(DgItemsShouldReturn);
        }

        /// <summary>
        /// Check application release log from GitHub and check with InstalledVersion in user.config
        /// If they are different, show release note and update InstalledVersion
        /// </summary>
        private async void CheckAppReleaseNote()
        {
            var github = new GitHubClient(new ProductHeaderValue("ACMC-Library-System"));
            var releases = await github.Repository.Release.GetAll("gh0stter", "ACMC-Library-System");
            var latestRelease = releases[0];
            string installedVersion = Properties.Settings.Default.InstalledVersion;
            if (installedVersion == latestRelease.TagName)
            {
                return;
            }
            string currentInstalledVersion;
            try
            {
                currentInstalledVersion = ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString();
            }
            catch (InvalidDeploymentException)
            {
                currentInstalledVersion = "Unknown";
            }
            Properties.Settings.Default.InstalledVersion = currentInstalledVersion;
            Properties.Settings.Default.Save();
            VisualHelper.ApplyBlurEffect(this);
            var releaseNoteWindow = new ReleaseNotes(latestRelease);
            releaseNoteWindow.ShowDialog();
            VisualHelper.ClearBlurEffect(this);
        }

        #endregion

        #region UI Logics

        #region Main UI Logics

        public MainWindow()
        {
            //boot and prepare data before initialize the main window
            var boot = new BootStrap();
            if (!boot.AppInitialized)
            {
                System.Windows.Application.Current.Shutdown();
                return;
            }
            boot.ShowDialog();
            if (!boot.AppPreparationSuccessfully)
            {
                System.Windows.Application.Current.Shutdown();
                return;
            }
            InitializeComponent();
            DataContext = this;
            Activate();
            // focus search box in the first run
            TbSearch.Focus();
            CheckAppReleaseNote();
        }

        //Tab selection change event, disable member/item editing
        private void OnTabSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(e.OriginalSource is TabControl))
            {
                return;
            }
            if (((TabItem)e.AddedItems[0]).Name == "TabMember" && ((TabItem)e.RemovedItems[0]).Name == "TabItem")
            {
                SelectedMember = (patron)DgMemberGrid.SelectedItem;
            }
            if (((TabItem)e.AddedItems[0]).Name == "TabItem" && ((TabItem)e.RemovedItems[0]).Name == "TabMember")
            {
                SelectedItem = (item)DgItemGrid.SelectedItem;
            }
            IsMemberEditMode = false;
            IsItemEditMode = false;
            _isAddingNewMember = false;
            IsAddingNewItem = false;
        }

        /// <summary>
        /// Refresh member/item grid data source
        /// </summary>
        /// <param name="targetGrid"></param>
        /// <param name="refreshCache"></param>
        private async Task RefreshGridSource(ItemsControl targetGrid, bool refreshCache = true)
        {
            if (refreshCache)
            {
                await Cache.RefreshMainCache();
            }
            switch (targetGrid.Name)
            {
                case "DgMemberGrid":
                    OnPropertyChanged("MemberList");
                    break;
                case "DgItemGrid":
                    OnPropertyChanged("ItemList");
                    break;
                case "DgActionHistory":
                    OnPropertyChanged("ActionHistories");
                    ScrollGridToIndex((DataGrid)targetGrid, 0);
                    break;
                case "DgItemsShouldReturn":
                    OnPropertyChanged("ItemsShouldReturn");
                    ScrollGridToIndex((DataGrid)targetGrid, 0);
                    break;
            }
        }

        /// <summary>
        /// Scroll data grid to provided index
        /// </summary>
        /// <param name="targetGrid"></param>
        /// <param name="index"></param>
        private static void ScrollGridToIndex(DataGrid targetGrid, int index)
        {
            Task.Delay(1000); //wait for list finish loading, not a proper way to do it
            targetGrid.SelectedIndex = index;
            targetGrid.UpdateLayout();
            targetGrid.ScrollIntoView(targetGrid.SelectedItem);
        }

        /// <summary>
        /// Open App Settings Window
        /// </summary>
        private void OpenAppSettingsWindow()
        {
            VisualHelper.ApplyBlurEffect(this);
            var appSettingWindow = new AppSettingWindow();
            appSettingWindow.ShowDialog();
            VisualHelper.ClearBlurEffect(this);
            Activate();
        }

        //Search button click event
        private async void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            // local valuables 
            string searchString = TbSearch.Text;
            // do nothing when the search string is empty
            if (string.IsNullOrWhiteSpace(searchString))
            {
                return;
            }
            // show the search results
            PrSearchingProgress.Visibility = Visibility.Visible;
            var memberSqlResults = new List<patron>();
            var itemSqlResult = new List<item>();
            var memberSearchResults = new ConcurrentBag<SearchResult>();
            var itemSearchResults = new ConcurrentBag<SearchResult>();
            var searchResults = new List<SearchResult>();
            try
            {
                var memberSearchTask = Task.Run(() =>
                {
                    using (var context = new LibraryDb())
                    {
                        memberSqlResults = (from member in context.patron
                                            where member.id.ToString().Contains(searchString) ||
                                                  member.barcode.Contains(searchString) ||
                                                  member.firstnames_en.Contains(searchString) ||
                                                  member.firstnames_ch.Contains(searchString) ||
                                                  member.surname_en.Contains(searchString) ||
                                                  member.surname_ch.Contains(searchString)
                                            select member).ToList();
                    }
                });
                var itemSearchTask = Task.Run(() =>
                {
                    using (var context = new LibraryDb())
                    {
                        itemSqlResult = (from item in context.item
                                         where item.id.ToString().Contains(searchString) ||
                                               item.title.Contains(searchString) ||
                                               item.isbn.Contains(searchString) ||
                                               item.barcode.Contains(searchString)
                                         select item).ToList();
                    }
                });
                await Task.WhenAll(memberSearchTask, itemSearchTask);
                var mapMemberResultToSearchResult = new ActionBlock<patron>(member =>
                {
                    var temp = new SearchResult
                    {
                        RecordType = SearchResultTypes.Member,
                        MemberId = member.id,
                        FirstNameCh = member.firstnames_ch,
                        LastNameCh = member.surname_ch,
                        FirstNameEn = member.firstnames_en,
                        LastNameEn = member.surname_en,
                        Img = (member.picture == null || member.picture.Length == 0) ? GetImageBytes(_currentDirectoryPath + NoPictureImgPath) : member.picture
                    };
                    if (!string.IsNullOrWhiteSpace(member.DisplayNameCh))
                    {
                        temp.FirstDisplayInfo = member.DisplayNameCh;
                    }
                    if (!string.IsNullOrWhiteSpace(member.DisplayNameEn))
                    {
                        temp.SecondDisplayInfo = member.DisplayNameEn;
                    }
                    memberSearchResults.Add(temp);
                }, _tplOption);
                var mapItemResultToSearchResult = new ActionBlock<item>(item =>
                {
                    var temp = new SearchResult
                    {
                        RecordType = SearchResultTypes.Item,
                        Img = GetImageBytes(GetItemImgPath(item.item_subclass)),
                        ItemId = item.id,
                        Title = item.title,
                        Barcode = item.barcode,
                        Isbn = item.isbn
                    };
                    temp.FirstDisplayInfo = temp.Title;
                    temp.SecondDisplayInfo = $"ID: {temp.ItemId} Barcode: {temp.Barcode}";
                    itemSearchResults.Add(temp);
                }, _tplOption);
                foreach (var member in memberSqlResults)
                {
                    await mapMemberResultToSearchResult.SendAsync(member);
                }
                foreach (var item in itemSqlResult)
                {
                    await mapItemResultToSearchResult.SendAsync(item);
                }
                mapMemberResultToSearchResult.Complete();
                mapItemResultToSearchResult.Complete();
                await mapMemberResultToSearchResult.Completion;
                await mapItemResultToSearchResult.Completion;
                searchResults.AddRange(memberSearchResults.OrderBy(i => i.MemberId));
                searchResults.AddRange(itemSearchResults.OrderBy(i => i.ItemId));
            }
            catch (Exception ex)
            {
                PrSearchingProgress.Visibility = Visibility.Hidden;
                Logger.Error(ex, $"Error on searching {searchString}.");
                await this.ShowMessageAsync("Error", $"{ex.Message}, please contact administrator.");
            }

            PrSearchingProgress.Visibility = Visibility.Hidden;
            if (searchResults.Count > 0)
            {
                if (searchResults.Count == 1)
                {
                    switch (searchResults[0].RecordType)
                    {
                        case SearchResultTypes.Member:
                            TbMemberFilter.Text = string.Empty;
                            int memberId = searchResults[0].MemberId;
                            TabMember.IsSelected = true;
                            ScrollGridToIndex(DgMemberGrid, MemberList.FindIndex(member => member.id == memberId));
                            break;
                        case SearchResultTypes.Item:
                            TbItemFilter.Text = string.Empty;
                            int itemId = searchResults[0].ItemId;
                            TabItem.IsSelected = true;
                            ScrollGridToIndex(DgItemGrid, ItemList.FindIndex(item => item.id == itemId));
                            break;
                    }
                }
                else
                {
                    VisualHelper.ApplyBlurEffect(this);
                    try
                    {
                        var searchResultWindow = new SearchResultWindow(searchResults);
                        searchResultWindow.ShowDialog();
                        VisualHelper.ClearBlurEffect(this);
                    }
                    catch (Exception exception)
                    {
                        Logger.Error(exception, $"Error on searching {searchString}.");
                        await this.ShowMessageAsync("Error", $"{exception.Message}, please contact administrator.");
                    }
                }
            }
            else
            {
                var messageResult = await this.ShowMessageAsync("Info", "Sorry, there is no result for this search. please try again.");
                if (messageResult != MessageDialogResult.Affirmative)
                {
                    return;
                }
                TbSearch.Focus();
            }
        }

        //Navigate to member detail from history
        private async void BtnHistoryNavToMember_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var history = ((Rectangle)sender).DataContext as action_history;
                if (history == null)
                {
                    throw new InvalidCastException("Cast -> null value.");
                }
                int index = MemberList.FindIndex(member => member.id == history.patronid);
                if (index == -1)
                {
                    throw new EntryPointNotFoundException("Unable to find Member.");
                }
                TabMember.IsSelected = true;
                ScrollGridToIndex(DgMemberGrid, index);
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Error on Navigating to Member");
                await this.ShowMessageAsync("Error", exception.Message);
            }
        }

        //Navigate to item detail from history
        private async void BtnHistoryNavToItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var history = ((Rectangle)sender).DataContext as action_history;
                if (history == null)
                {
                    throw new InvalidCastException("Cast -> null value.");
                }
                int index = ItemList.FindIndex(item => item.id == history.itemid);
                if (index == -1)
                {
                    throw new EntryPointNotFoundException("Unable to find item.");
                }
                TabItem.IsSelected = true;
                ScrollGridToIndex(DgItemGrid, index);
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Error on Navigating to item");
                await this.ShowMessageAsync("Error", exception.Message);
            }
        }

        //Refresh history
        private async void HistoryGridRefreshIconClick(object sender, MouseButtonEventArgs e)
        {
            await RefreshGridSource(DgActionHistory);
            ScrollGridToIndex(DgActionHistory, 0);
            NotificationBar.Infomation("Refresh successfully.", NotificationFadeTimeInSecond);
        }

        //Refresh items should return
        private async void ItemsShouldReturnGridRefreshIconClick(object sender, MouseButtonEventArgs e)
        {
            await RefreshGridSource(DgItemsShouldReturn);
            ScrollGridToIndex(DgItemsShouldReturn, 0);
            NotificationBar.Infomation("Refresh successfully.", NotificationFadeTimeInSecond);
        }

        //Navigate to Member detail
        private async void BtnItemShouldReturnNavToMember_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var record = ((Rectangle)sender).DataContext as item;
                if (record == null)
                {
                    throw new InvalidCastException("Cast -> null value.");
                }
                int index = MemberList.FindIndex(member => member.id == record.patronid);
                if (index == -1)
                {
                    throw new EntryPointNotFoundException("Unable to find Member.");
                }
                TabMember.IsSelected = true;
                ScrollGridToIndex(DgMemberGrid, index);
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Error on Navigating to Member");
                await this.ShowMessageAsync("Error", exception.Message);
            }
        }

        //Navigate to item detail
        private async void BtnItemShouldReturnNavToItem_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var record = ((Rectangle)sender).DataContext as item;
                if (record == null)
                {
                    throw new InvalidCastException("Cast -> null value.");
                }
                int index = ItemList.FindIndex(item => item.id == record.id);
                if (index == -1)
                {
                    throw new EntryPointNotFoundException("Unable to find item.");
                }
                TabItem.IsSelected = true;
                ScrollGridToIndex(DgItemGrid, index);
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Error on Navigating to item");
                await this.ShowMessageAsync("Error", exception.Message);
            }
        }

        //Open app setting window
        private void AppSettingsIconClick(object sender, RoutedEventArgs e)
        {
            OpenAppSettingsWindow();
        }

        private void NavigateMemberTabIconClick(object sender, RoutedEventArgs e)
        {
            TabMember.IsSelected = true;
        }

        private void NavigateItemTabIconClick(object sender, RoutedEventArgs e)
        {
            TabItem.IsSelected = true;
        }

        private void OpenAppSettingsIconClick(object sender, RoutedEventArgs e)
        {
            OpenAppSettingsWindow();
        }

        #endregion

        #region Member Tab Logics

        /// <summary>
        /// Save Member to database
        /// </summary>
        private async void SaveMemberHandler()
        {
            if (_isAddingNewMember)
            {
                try
                {
                    using (var context = new LibraryDb())
                    {
                        using (var transactionScope = new TransactionScope())
                        {
                            //might be can use context.patron.AddOrUpdate();
                            if (SelectedMember.id == -1)
                            {
                                if (IsValidMember(SelectedMember))
                                {
                                    context.patron.Add(SelectedMember);
                                }
                                else
                                {
                                    await this.ShowMessageAsync("Info", "Please provide valid Name and Barcode.");
                                    return;
                                }
                            }
                            else
                            {
                                throw new InvalidDataException("Invalid Member.");
                            }
                            context.SaveChanges();
                            transactionScope.Complete();
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Error on adding new Member.");
                    await this.ShowMessageAsync("Error", "Error on adding new Member to the database, please contact administrator.");
                    IsMemberEditMode = false;
                    await RefreshGridSource(DgMemberGrid);
                    ScrollGridToIndex(DgMemberGrid, 0);
                    return;
                }
                await RefreshGridSource(DgMemberGrid);
                ScrollGridToIndex(DgMemberGrid, DgMemberGrid.Items.Count - 1);
                IsMemberEditMode = false;
                _isAddingNewMember = false;
                NotificationBar.Success("Member has been added.", NotificationFadeTimeInSecond);
            }
            else
            {
                if (IsMemberChanged(SelectedMember))
                {
                    try
                    {
                        using (var context = new LibraryDb())
                        {
                            using (var transactionScope = new TransactionScope())
                            {
                                var memberInDb = context.patron.FirstOrDefault(member => member.id == SelectedMember.id);
                                if (memberInDb == null)
                                {
                                    throw new EntryPointNotFoundException("Unable to find selected Member.");
                                }
                                context.Entry(memberInDb).CurrentValues.SetValues(SelectedMember);
                                context.SaveChanges();
                                transactionScope.Complete();
                            }
                        }

                    }
                    catch (Exception exception)
                    {
                        Logger.Error(exception, "Error on Saving Member.");
                        await this.ShowMessageAsync("Error", exception.Message);
                        ScrollGridToIndex(DgMemberGrid, 0);
                        return;
                    }
                    int currentItemIndex = MemberList.FindIndex(member => member.id == SelectedMember.id);
                    await RefreshGridSource(DgItemsShouldReturn);
                    await RefreshGridSource(DgMemberGrid, false);
                    ScrollGridToIndex(DgMemberGrid, currentItemIndex);
                    IsMemberEditMode = false;
                    OnPropertyChanged("SelectedMember");
                    NotificationBar.Success("Update successfully.", NotificationFadeTimeInSecond);
                }
                else
                {
                    IsMemberEditMode = false;
                }
            }
        }

        /// <summary>
        /// Handle unsaved Member
        /// </summary>
        private async void UnsavedMemberHandler()
        {
            if (IsMemberChanged(SelectedMember))
            {
                var result = await UnsavedDialog();
                if (result != MessageDialogResult.Affirmative)
                {
                    IsMemberEditMode = true;
                    return;
                }
            }
            //cancel saving, restore last selection
            if (SelectedMember.id == -1)
            {
                ScrollGridToIndex(DgMemberGrid, 0);
            }
            else
            {
                SelectedMember = MemberList.First(i => i.id == SelectedMember.id);
            }
            _isAddingNewMember = false;
            IsMemberEditMode = false;
            OnPropertyChanged("SelectedMember");
        }

        //Member filter text change event
        private async void MemberFilterChanged(object sender, TextChangedEventArgs e)
        {
            if (IsMemberEditMode)
            {
                if (IsMemberChanged(SelectedMember))
                {
                    await this.ShowMessageAsync("Warning", "Please save your changes before searching member.");
                    return;
                }
                IsMemberEditMode = false;
            }
            _memberFilter = TbMemberFilter.Text;
            OnPropertyChanged("MemberList");
            SelectedMember = (patron)DgMemberGrid.SelectedItem;
        }

        //Refresh Member grid
        private async void MemberGridRefreshIconClick(object sender, MouseButtonEventArgs e)
        {
            if (IsMemberEditMode)
            {
                if (IsMemberChanged(SelectedMember))
                {
                    await this.ShowMessageAsync("Warning", "Please save your changes before refreshing grid.");
                    return;
                }
                IsMemberEditMode = false;
            }
            TbMemberFilter.Clear();
            await RefreshGridSource(DgMemberGrid);
            ScrollGridToIndex(DgMemberGrid, 0);
            NotificationBar.Infomation("Refresh successfully.", NotificationFadeTimeInSecond);
        }

        //User click on data grid
        private async void MemberGridPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (IsMemberEditMode)
            {
                if (IsMemberChanged(SelectedMember))
                {
                    e.Handled = true;
                    await this.ShowMessageAsync("Warning", "Please save your changes before navigating to another Member.");
                    return;
                }
                IsMemberEditMode = false;
            }
            e.Handled = false;
        }

        //User use keyboard to navigate on data grid
        private async void MemberGridPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (IsMemberEditMode)
            {
                if (IsMemberChanged(SelectedMember))
                {
                    e.Handled = true;
                    await this.ShowMessageAsync("Warning", "Please save your changes before navigating to another Member.");
                    return;
                }
                IsMemberEditMode = false;
            }
            e.Handled = false;
        }

        //User grid selection change event
        private void MemberGridSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedMember = (patron)DgMemberGrid.SelectedItem;
        }

        //Add new Member
        private void AddMemberIconClick(object sender, MouseButtonEventArgs e)
        {
            if (_isAddingNewMember)
            {
                return;
            }
            DgMemberGrid.SelectedItem = null;
            var newMember = new patron
            {
                id = -1,
                limit = BusinessRules.DefaultQuotaPerMember,
                created = DateTime.Today,
                expiry = DateTime.Today.AddYears(1)
            };
            SelectedMember = newMember;
            IsMemberEditMode = true;
            _isAddingNewMember = true;
        }

        //Edit mode icon click
        private void MemberEditModeIconClick(object sender, MouseButtonEventArgs e)
        {
            if (IsMemberEditMode)
            {
                UnsavedMemberHandler();
            }
            else
            {
                IsMemberEditMode = true;
            }
        }

        //Edit mode toggle icon click
        private void ToggleMemberEditMode_Click(object sender, RoutedEventArgs e)
        {
            if (IsMemberEditMode)
            {
                UnsavedMemberHandler();
            }
            else
            {
                IsMemberEditMode = true;
            }
        }

        //Navigate to borrowed item detail
        private async void BtnNavToItem_Click(object sender, RoutedEventArgs routedEventArgs)
        {
            try
            {
                var tryToViewItem = ((Rectangle)sender).DataContext as item;
                if (tryToViewItem == null)
                {
                    throw new InvalidCastException("Cast -> null value.");
                }
                int index = ItemList.FindIndex(item => item.id == tryToViewItem.id);
                if (index == -1)
                {
                    throw new EntryPointNotFoundException("Unable to find item.");
                }
                TabItem.IsSelected = true;
                ScrollGridToIndex(DgItemGrid, index);
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Error on Navigating to item");
                await this.ShowMessageAsync("Error", exception.Message);
            }
        }

        //Renew selected borrowed item
        private async void BtnMemberRenewItem_Click(object sender, RoutedEventArgs routedEventArgs)
        {
            try
            {
                var tryToRenewItem = ((Rectangle)sender).DataContext as item;
                int selectIndex = SelectedMember.BorrowingItems.IndexOf(tryToRenewItem);
                if (tryToRenewItem == null)
                {
                    throw new InvalidCastException("Cast -> null value.");
                }
                using (var context = new LibraryDb())
                {
                    var itemInDb = context.item.FirstOrDefault(item => item.id == tryToRenewItem.id);
                    if (itemInDb == null)
                    {
                        throw new EntryPointNotFoundException("Unable to find selected item.");
                    }
                    using (var transactionScope = new TransactionScope())
                    {
                        itemInDb.Renew();
                        AddActionHistory(context, SelectedMember.id, itemInDb.id, action_type.ActionTypeEnum.Renew);
                        context.SaveChanges();
                        SelectedMember.BorrowingItems = context.item.Where(item => item.patronid == SelectedMember.id).ToList();
                        transactionScope.Complete();
                        DgCurrentBorrowingItem.Items.Refresh();
                    }
                    if (SelectedItem.id == itemInDb.id)
                    {
                        SelectedItem.Renew();
                        OnPropertyChanged("SelectedItem");
                    }
                }
                OnPropertyChanged("SelectedMember");
                DgCurrentBorrowingItem.SelectedIndex = SelectedMember.BorrowingItems.Count > selectIndex ? selectIndex : SelectedMember.BorrowingItems.Count - 1;
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Error on Renewing item");
                await this.ShowMessageAsync("Error", exception.Message);
            }
            NotificationBar.Success("Item has been renewed.", NotificationFadeTimeInSecond);
        }

        //Return selected borrowed item
        private async void BtnMemberReturnItem_Click(object sender, RoutedEventArgs routedEventArgs)
        {
            try
            {
                var tryToReturnItem = ((Rectangle)sender).DataContext as item;
                int selectIndex = SelectedMember.BorrowingItems.IndexOf(tryToReturnItem);
                if (tryToReturnItem == null)
                {
                    throw new InvalidCastException("Cast -> null value.");
                }
                using (var context = new LibraryDb())
                {
                    var itemInDb = context.item.FirstOrDefault(item => item.id == tryToReturnItem.id);
                    if (itemInDb == null)
                    {
                        throw new EntryPointNotFoundException("Unable to find selected item.");
                    }
                    using (var transactionScope = new TransactionScope())
                    {
                        itemInDb.Return();
                        AddActionHistory(context, SelectedMember.id, itemInDb.id, action_type.ActionTypeEnum.Return);
                        context.SaveChanges();
                        SelectedMember.BorrowingItems = context.item.Where(item => item.patronid == SelectedMember.id).ToList();
                        transactionScope.Complete();
                    }
                    if (SelectedItem.id == itemInDb.id)
                    {
                        SelectedItem.patronid = null;
                        OnPropertyChanged("SelectedItem");
                    }
                }
                OnPropertyChanged("SelectedMember");
                DgCurrentBorrowingItem.SelectedIndex = SelectedMember.BorrowingItems.Count > selectIndex ? selectIndex : SelectedMember.BorrowingItems.Count - 1;
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Error on Returning item");
                await this.ShowMessageAsync("Error", exception.Message);
            }
            NotificationBar.Success("Item has been returned.", NotificationFadeTimeInSecond);
        }

        //Borrow item to selected Member
        private async void BtnMemberBorrowItem_Click(object sender, RoutedEventArgs e)
        {
            string dialogResult = await this.ShowInputAsync("Borrow Item", "Please enter item Barcode");
            if (string.IsNullOrWhiteSpace(dialogResult))
            {
                return;
            }
            if (!int.TryParse(dialogResult, out _))
            {
                await this.ShowMessageAsync("Error", "Invalid barcode, please try again.");
                return;
            }
            try
            {
                using (var context = new LibraryDb())
                {
                    var itemInDb = context.item.FirstOrDefault(item => item.barcode == dialogResult);
                    if (itemInDb == null)
                    {
                        await this.ShowMessageAsync("Error", "Unable to find item in database, please try again.");
                        return;
                    }
                    var currentBorrower = context.patron.FirstOrDefault(member => member.id == itemInDb.patronid);
                    if (currentBorrower == null)
                    {
                        using (var transactionScope = new TransactionScope())
                        {
                            itemInDb.IssueToMember(SelectedMember);
                            SelectedMember.BorrowItem(itemInDb);
                            AddActionHistory(context, SelectedMember.id, itemInDb.id, action_type.ActionTypeEnum.Lend);
                            context.SaveChanges();
                            transactionScope.Complete();
                        }
                        DgCurrentBorrowingItem.Items.Refresh();
                        OnPropertyChanged("SelectedMember");
                        if (SelectedItem.id == itemInDb.id)
                        {
                            SelectedItem.patronid = SelectedMember.id;
                            OnPropertyChanged("SelectedItem");
                        }
                    }
                    else
                    {
                        var result = await NavigateDialog($"{currentBorrower.DisplayNameTitle} is holding this item currently, click OK navigate to item detail.");
                        if (result != MessageDialogResult.Affirmative)
                        {
                            return;
                        }
                        TabItem.IsSelected = true;
                        int index = ItemList.FindIndex(item => item.id == itemInDb.id);
                        if (index == -1)
                        {
                            throw new EntryPointNotFoundException("Unable to find item.");
                        }
                        ScrollGridToIndex(DgItemGrid, index);
                    }
                }
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Error on Borrowing item");
                await this.ShowMessageAsync("Error", exception.Message);
            }
            NotificationBar.Success("Item has been issued.", NotificationFadeTimeInSecond);
        }

        //Same Member changes
        private void BtnSaveMember_Click(object sender, RoutedEventArgs e)
        {
            SaveMemberHandler();
        }

        //Renew Member
        private async void BtnRenewMember_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedMember.id == -1)
            {
                return;
            }
            try
            {
                using (var context = new LibraryDb())
                {
                    using (var transactionScope = new TransactionScope())
                    {
                        var memberInDb = context.patron.FirstOrDefault(member => member.id == SelectedMember.id);
                        if (memberInDb == null)
                        {
                            throw new EntryPointNotFoundException("Unable to find selected Member.");
                        }
                        memberInDb.Renew();
                        context.SaveChanges();
                        transactionScope.Complete();
                    }
                }
                await Cache.RefreshMainCache();
                SelectedMember = MemberList.First(i => i.id == SelectedMember.id);
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Error on Renewing Member.");
                await this.ShowMessageAsync("Error", exception.Message);
            }
            NotificationBar.Success("Member has been renewed.", NotificationFadeTimeInSecond);
        }

        //Cancel Member editing
        private void BtnCancelEditingMember_Click(object sender, RoutedEventArgs e)
        {
            UnsavedMemberHandler();
        }

        //Delete selected Member
        private async void BtnDeleteMember_Click(object sender, RoutedEventArgs e)
        {
            if (_isAddingNewMember)
            {
                var result = await UnsavedDialog();
                if (result == MessageDialogResult.Affirmative)
                {
                    IsMemberEditMode = false;
                    _isAddingNewMember = false;
                    await RefreshGridSource(DgMemberGrid);
                    ScrollGridToIndex(DgMemberGrid, 0);
                }
                else
                {
                    IsMemberEditMode = true;
                }
            }
            else
            {
                try
                {
                    using (var context = new LibraryDb())
                    {
                        using (var transactionScope = new TransactionScope())
                        {
                            var memberInDb = context.patron.FirstOrDefault(member => member.id == SelectedMember.id);
                            if (memberInDb == null)
                            {
                                throw new EntryPointNotFoundException("Unable to find selected Member.");
                            }
                            if (!memberInDb.AllowToDelete)
                            {
                                throw new InvalidOperationException("Cannot delete this Member.");
                            }
                            var result = await this.ShowMessageAsync("Delete Confirmation",
                                                                     $"Are you sure you want to DELETE this Member?{Environment.NewLine}This CANNOT be undone!",
                                                                     MessageDialogStyle.AffirmativeAndNegative,
                                                                     _binarySelectionDialogSettings);
                            if (result != MessageDialogResult.Affirmative)
                            {
                                return;
                            }
                            if (context.item.Any(item => item.patronid == memberInDb.id))
                            {
                                await this.ShowMessageAsync("Warning", "Cannot delete this use, because this Member still holding some books/items from library.");
                                return;
                            }
                            context.patron.Where(member => member.id == SelectedMember.id).Delete();
                            context.SaveChanges();
                            transactionScope.Complete();
                        }
                    }
                    IsMemberEditMode = false;
                }
                catch (Exception exception)
                {
                    Logger.Error(exception, "Error on Deleting Member.");
                    await RefreshGridSource(DgMemberGrid);
                    await this.ShowMessageAsync("Error", exception.Message);
                }
                NotificationBar.Warning("Member has been deleted.", NotificationFadeTimeInSecond);
            }
        }

        //Select photo
        private void BtnSelectPhoto_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                DefaultExt = ".png",
                Filter = "PNG Files (*.png)|*.png|JPG Files (*.jpg)|*.jpg|JPEG Files (*.jpeg)|*.jpeg|GIF Files (*.gif)|*.gif"
            };
            var result = dialog.ShowDialog();
            // Get the selected file name and display in a TextBox 
            if (result != true)
            {
                return;
            }
            string filePath = dialog.FileName;
            SelectedMember.picture = GetImageBytes(filePath);
            OnPropertyChanged("SelectedMember");
        }

        //Delete current Member photo
        private void DeleteMemberPhotoClick(object sender, MouseButtonEventArgs e)
        {
            SelectedMember.picture = null;
            OnPropertyChanged("SelectedMember");
        }

        #endregion

        #region Item Tab Logics

        /// <summary>
        /// Save item to database
        /// </summary>
        private async void SaveItemHandler()
        {
            if (IsAddingNewItem)
            {
                try
                {
                    using (var context = new LibraryDb())
                    {
                        using (var transactionScope = new TransactionScope())
                        {
                            //might be can use context.patron.AddOrUpdate();
                            if (SelectedItem.id == -1)
                            {
                                if (IsValidItem(SelectedItem))
                                {
                                    context.item.Add(SelectedItem);
                                }
                                else
                                {
                                    await this.ShowMessageAsync("Info", "Please provide valid Barcode/Code/Title/Category/Class/Status.");
                                    return;
                                }
                            }
                            else
                            {
                                throw new InvalidDataException("Invalid item.");
                            }
                            context.SaveChanges();
                            transactionScope.Complete();
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Error on adding new item.");
                    await this.ShowMessageAsync("Error", "Error on adding new item to the database, please contact administrator.");
                    await RefreshGridSource(DgItemGrid);
                    ScrollGridToIndex(DgItemGrid, 0);
                    return;
                }
                await RefreshGridSource(DgItemsShouldReturn);
                await RefreshGridSource(DgItemGrid, false);
                IsItemEditMode = false;
                IsAddingNewItem = false;
                ScrollGridToIndex(DgItemGrid, DgItemGrid.Items.Count - 1);
                NotificationBar.Success("Item has been added.", NotificationFadeTimeInSecond);
            }
            else
            {
                if (IsItemChanged(SelectedItem))
                {
                    try
                    {
                        using (var context = new LibraryDb())
                        {
                            using (var transactionScope = new TransactionScope())
                            {
                                var itemInDb = context.item.FirstOrDefault(i => i.id == SelectedItem.id);
                                if (itemInDb == null)
                                {
                                    throw new EntryPointNotFoundException("Unable to find selected item.");
                                }
                                context.Entry(itemInDb).CurrentValues.SetValues(SelectedItem);
                                context.SaveChanges();
                                transactionScope.Complete();
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        Logger.Error(exception, "Error on Saving item.");
                        await this.ShowMessageAsync("Error", exception.Message);
                        ScrollGridToIndex(DgItemGrid, 0);
                        return;
                    }
                    int currentItemIndex = ItemList.FindIndex(item => item.id == SelectedItem.id);
                    await RefreshGridSource(DgItemsShouldReturn);
                    await RefreshGridSource(DgItemGrid, false);
                    ScrollGridToIndex(DgItemGrid, currentItemIndex);
                    IsItemEditMode = false;
                    OnPropertyChanged("SelectedItem");
                    if (SelectedMember.id == SelectedItem.patronid)
                    {
                        SelectedMember.BorrowingItems = ItemList.Where(i => i.patronid == SelectedMember.id).ToList();
                        OnPropertyChanged("SelectedMember");
                    }
                    NotificationBar.Success("Update successfully.", NotificationFadeTimeInSecond);
                }
                else
                {
                    IsItemEditMode = false;
                }
            }
        }

        /// <summary>
        /// Handle unsaved item
        /// </summary>
        private async void UnsavedItemHandler()
        {
            if (IsItemChanged(SelectedItem))
            {
                var result = await UnsavedDialog();
                if (result != MessageDialogResult.Affirmative)
                {
                    IsItemEditMode = true;
                    return;
                }
            }
            //cancel saving, restore last selection
            if (SelectedItem.id == -1)
            {
                ScrollGridToIndex(DgItemGrid, 0);
            }
            else
            {
                SelectedItem = ItemList.First(i => i.id == SelectedItem.id);
            }
            IsAddingNewItem = false;
            IsItemEditMode = false;
            OnPropertyChanged("SelectedItem");
        }

        //Item filter text change event
        private async void ItemFilterChanged(object sender, TextChangedEventArgs e)
        {
            if (IsItemEditMode)
            {
                if (IsItemChanged(SelectedItem))
                {
                    e.Handled = true;
                    await this.ShowMessageAsync("Warning", "Please save your changes before searching item.");
                    return;
                }
                IsItemEditMode = false;
            }
            _itemFilter = TbItemFilter.Text;
            OnPropertyChanged("ItemList");
            SelectedItem = (item)DgItemGrid.SelectedItem;
        }

        //Refresh item grid
        private async void ItemGridRefreshIconClick(object sender, MouseButtonEventArgs e)
        {
            if (IsItemEditMode)
            {
                if (IsItemChanged(SelectedItem))
                {
                    await this.ShowMessageAsync("Warning", "Please save your changes before refreshing grid.");
                    return;
                }
                IsItemEditMode = false;
            }
            TbItemFilter.Clear();
            await RefreshGridSource(DgItemGrid);
            ScrollGridToIndex(DgItemGrid, 0);
            NotificationBar.Infomation("Refresh successfully.", NotificationFadeTimeInSecond);
        }

        //User click on data grid
        private async void ItemGridPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (IsItemEditMode)
            {
                if (IsItemChanged(SelectedItem))
                {
                    e.Handled = true;
                    await this.ShowMessageAsync("Warning", "Please save your changes before navigating to another item.");
                    return;
                }
                IsItemEditMode = false;
            }
            e.Handled = false;
        }

        //User use keyboard to navigate on data grid
        private async void ItemGridPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (IsItemEditMode)
            {
                if (IsItemChanged(SelectedItem))
                {
                    e.Handled = true;
                    await this.ShowMessageAsync("Warning", "Please save your changes before navigating to another item.");
                    return;
                }
                IsItemEditMode = false;
            }
            e.Handled = false;
        }

        //Item grid selection change event
        private void ItemGridSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedItem = (item)DgItemGrid.SelectedItem;
        }

        //Add new item
        private void AddItemIconClick(object sender, MouseButtonEventArgs e)
        {
            if (IsAddingNewItem)
            {
                return;
            }
            DgItemGrid.SelectedItem = null;
            var newItem = new item
            {
                id = -1
            };
            SelectedItem = newItem;
            IsItemEditMode = true;
            IsAddingNewItem = true;
        }

        //Edit mode icon click
        private void ItemEditModeIconClick(object sender, MouseButtonEventArgs e)
        {
            if (IsItemEditMode)
            {
                UnsavedItemHandler();
            }
            else
            {
                IsItemEditMode = true;
            }
        }

        //Edit mode toggle icon click
        private void ToggleItemEditMode_Click(object sender, RoutedEventArgs e)
        {
            if (IsItemEditMode)
            {
                UnsavedItemHandler();
            }
            else
            {
                IsItemEditMode = true;
            }
        }

        //Issue an item to Member
        private async void BtnIssueToMember_Click(object sender, RoutedEventArgs e)
        {
            string enteredMemberBarcode = TbIssueToMemberBarcode.Text.Trim();
            if (string.IsNullOrWhiteSpace(enteredMemberBarcode))
            {
                return;
            }
            try
            {
                using (var context = new LibraryDb())
                {
                    var itemInDb = context.item.First(i => i.id == SelectedItem.id);
                    var memberInDb = MemberList.FirstOrDefault(i => i.barcode.Equals(enteredMemberBarcode, StringComparison.InvariantCultureIgnoreCase));
                    if (memberInDb == null)
                    {
                        throw new EntryPointNotFoundException("Unable to find item in database.");
                    }
                    memberInDb.BorrowingItems = ItemList.Where(i => i.patronid == memberInDb.id).ToList();
                    if (!memberInDb.CanBorrowItem)
                    {
                        var result = await NavigateDialog($"{memberInDb.DisplayNameTitle} Cannot borrow more items.{Environment.NewLine}" +
                                                          $"Reason: {memberInDb.UnableToBorrowItemReason + Environment.NewLine}" +
                                                          "click OK navigate to member details.");
                        if (result != MessageDialogResult.Affirmative)
                        {
                            return;
                        }
                        TabMember.IsSelected = true;
                        int index = MemberList.FindIndex(i => i.id == memberInDb.id);
                        if (index == -1)
                        {
                            throw new EntryPointNotFoundException("Unable to find item.");
                        }
                        ScrollGridToIndex(DgMemberGrid, index);
                        return;
                    }
                    using (var transactionScope = new TransactionScope())
                    {
                        itemInDb.IssueToMember(memberInDb);
                        AddActionHistory(context, memberInDb.id, itemInDb.id, action_type.ActionTypeEnum.Lend);
                        context.SaveChanges();
                        transactionScope.Complete();
                    }
                    SelectedItem = itemInDb;
                    TbIssueToMemberBarcode.Text = string.Empty;
                    if (SelectedMember.id == memberInDb.id)
                    {
                        SelectedMember.BorrowingItems.Add(itemInDb);
                        DgCurrentBorrowingItem.Items.Refresh();
                        OnPropertyChanged("SelectedMember");
                    }
                }
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Error on Issuing item to Member.");
                await this.ShowMessageAsync("Error", exception.Message);
            }
            NotificationBar.Success("Item has been issued.", NotificationFadeTimeInSecond);
        }

        //Navigate to view Member detail
        private async void BtnViewMemberDetail_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int index = MemberList.FindIndex(member => member.id == SelectedItem.patronid);
                if (index == -1)
                {
                    throw new EntryPointNotFoundException($"Unable to find Member ID: {SelectedItem.patronid} in the database.");
                }
                TabMember.IsSelected = true;
                ScrollGridToIndex(DgMemberGrid, index);
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Error on Viewing item borrower detail.");
                await this.ShowMessageAsync("Error", exception.Message);
            }
        }

        //renew current item
        private async void BtnRenewItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var context = new LibraryDb())
                {
                    using (var transactionScope = new TransactionScope())
                    {
                        var itemInDb = context.item.FirstOrDefault(item => item.id == SelectedItem.id);
                        if (itemInDb == null)
                        {
                            throw new EntryPointNotFoundException("Unable to find item.");
                        }
                        var memberInDb = context.patron.FirstOrDefault(member => member.id == itemInDb.patronid);
                        if (memberInDb == null)
                        {
                            throw new ArgumentException($"This item is lent to a Member ID: {itemInDb.patronid} that no longer exist in the database, please return this item instead.");
                        }
                        itemInDb.Renew();
                        SelectedItem.Renew();
                        AddActionHistory(context, SelectedItem.patronid, itemInDb.id, action_type.ActionTypeEnum.Renew);
                        context.SaveChanges();
                        transactionScope.Complete();
                    }
                }
                OnPropertyChanged("SelectedItem");
                if (SelectedMember.id == SelectedItem.patronid)
                {
                    SelectedMember.BorrowingItems = ItemList.Where(i => i.patronid == SelectedMember.id).ToList();
                    OnPropertyChanged("SelectedMember");
                }
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Error on Renewing item.");
                await this.ShowMessageAsync("Error", exception.Message);
            }
            NotificationBar.Success("Item has been renewed.", NotificationFadeTimeInSecond);
        }

        //Return current item
        private async void BtnReturnItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var borrowerId = SelectedItem.patronid;
                using (var context = new LibraryDb())
                {
                    using (var transactionScope = new TransactionScope())
                    {
                        var itemInDb = context.item.FirstOrDefault(item => item.id == SelectedItem.id);
                        if (itemInDb == null)
                        {
                            throw new EntryPointNotFoundException("Unable to find item.");
                        }
                        itemInDb.Return();
                        SelectedItem.Return();
                        AddActionHistory(context, borrowerId, itemInDb.id, action_type.ActionTypeEnum.Return);
                        context.SaveChanges();
                        transactionScope.Complete();
                    }
                }
                OnPropertyChanged("SelectedItem");
                if (SelectedMember.id != borrowerId)
                {
                    SelectedMember.BorrowingItems = ItemList.Where(i => i.patronid == SelectedMember.id).ToList();
                    OnPropertyChanged("SelectedMember");
                }
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Error on Returning item.");
                await this.ShowMessageAsync("Error", exception.Message);
            }
            NotificationBar.Success("Item has been returned.", NotificationFadeTimeInSecond);
        }

        //Update item
        private void BtnSaveItem_Click(object sender, RoutedEventArgs e)
        {
            SaveItemHandler();
        }

        //Cancel item editing
        private void BtnCancelEditingItem_Click(object sender, RoutedEventArgs e)
        {
            UnsavedItemHandler();
        }

        //Delete item
        private async void BtnDeleteItem_Click(object sender, RoutedEventArgs e)
        {
            if (IsAddingNewItem)
            {
                var result = await UnsavedDialog();
                if (result == MessageDialogResult.Affirmative)
                {
                    IsItemEditMode = false;
                    IsAddingNewItem = false;
                    await RefreshGridSource(DgItemGrid);
                    ScrollGridToIndex(DgItemGrid, 0);
                }
                else
                {
                    IsItemEditMode = true;
                }
            }
            else
            {
                try
                {
                    using (var context = new LibraryDb())
                    {
                        using (var transactionScope = new TransactionScope())
                        {
                            var itemInDb = context.item.FirstOrDefault(item => item.id == SelectedItem.id);
                            if (itemInDb == null)
                            {
                                throw new EntryPointNotFoundException("Unable to find selected item.");
                            }
                            var result = await this.ShowMessageAsync("Delete Confirmation",
                                                                     $"Are you sure you want to DELETE this item?{Environment.NewLine}This CANNOT be undone!",
                                                                     MessageDialogStyle.AffirmativeAndNegative,
                                                                     _binarySelectionDialogSettings);
                            if (result != MessageDialogResult.Affirmative)
                            {
                                return;
                            }
                            context.item.Where(item => item.id == SelectedItem.id).Delete();
                            context.SaveChanges();
                            transactionScope.Complete();
                        }
                    }
                    IsItemEditMode = false;
                }
                catch (Exception exception)
                {
                    Logger.Error(exception, "Error on Deleting item.");
                    await RefreshGridSource(DgItemGrid);
                    await this.ShowMessageAsync("Error", exception.Message);
                }
                finally
                {
                    if (SelectedMember.id == SelectedItem.patronid)
                    {
                        SelectedMember.BorrowingItems = ItemList.Where(i => i.patronid == SelectedMember.id).ToList();
                        OnPropertyChanged("SelectedMember");
                    }
                }
                NotificationBar.Warning("Item has been deleted.", NotificationFadeTimeInSecond);
            }
        }

        #endregion

        #endregion
    }
}