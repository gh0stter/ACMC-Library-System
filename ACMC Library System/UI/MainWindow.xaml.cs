using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ACMC_Library_System.DbModels;
using ACMC_Library_System.Supports;
using DomainModels.DataModel;
using DomainModels.ViewModel;
using EntityFramework.Extensions;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using NLog;
using Rectangle = System.Windows.Shapes.Rectangle;

namespace ACMC_Library_System.UI
{
    /// <summary>
    ///     MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : MetroWindow, INotifyPropertyChanged
    {
        #region Private Properties

        private readonly WindowBlurEffect _blurWorker = new WindowBlurEffect();
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private readonly string _currentDirectoryPath = Directory.GetCurrentDirectory();
        private const string NoPictureImgPath = @"\Resources\UI Icons\NoImg.png";
        private const string ItemDefaultImgPath = @"\Resources\UI Icons\ItemIcon.png";
        private const string ItemBookImgPath = @"\Resources\UI Icons\Book.png";
        private const string ItemCdImgPath = @"\Resources\UI Icons\CD.png";
        private const string ItemTapeImgPath = @"\Resources\UI Icons\Tape.png";

        private readonly MetroDialogSettings _binarySelectionDialogSettings = new MetroDialogSettings
        {
            AffirmativeButtonText = "Yes",
            NegativeButtonText = "No",
            DefaultButtonFocus = MessageDialogResult.Negative
        };

        private string _userFilter = string.Empty;
        private patron _selectedUser = new patron();

        private string _itemFilter = string.Empty;
        private item _selectedItem = new item();

        private bool _isUserEditingMode;
        private bool _isAddingNewUser;

        private bool _isItemEditingMode;
        private bool _isAddingNewItem;

        #endregion

        #region Public Properties

        public List<item_category> ItemCategories => Cache.ItemCategories;

        public List<item_class> ItemClasses => Cache.ItemClasses;

        public List<item_status> ItemStatuses => Cache.ItemStatuses;

        /// <summary>
        /// Getter, get user list from database
        /// </summary>
        public List<patron> UserList
        {
            get
            {
                return _userFilter.Trim() == string.Empty
                    ? Cache.Users
                    : Cache.Users.Where(user => user.id.ToString().Contains(_userFilter) ||
                                                user.barcode.IndexOf(_userFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                user.firstnames_ch?.IndexOf(_userFilter, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                user.firstnames_en?.IndexOf(_userFilter, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
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

        public patron SelectedUser
        {
            get { return _selectedUser; }
            set
            {
                if (value == null || AreSameUser(value, _selectedUser))
                {
                    return;
                }
                if (value.id == -1)
                {
                    _selectedUser = value;
                }
                else
                {
                    using (var context = new LibraryDb())
                    {
                        _selectedUser = context.patron.First(user => user.id == value.id);
                        _selectedUser.BorrowingItems = context.item.Where(item => item.patronid == _selectedUser.id).ToList();
                    }
                }
                OnPropertyChanged("SelectedUser");
            }
        }

        public bool IsUserEditMode
        {
            get { return _isUserEditingMode; }
            set
            {
                _isUserEditingMode = value;
                OnPropertyChanged("IsUserEditMode");
            }
        }

        public item SelectedItem
        {
            get { return _selectedItem; }
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
                        _selectedItem.Borrower = context.patron.FirstOrDefault(user => user.id == _selectedItem.patronid);
                    }
                }
                OnPropertyChanged("SelectedItem");
            }
        }

        public bool IsItemEditMode
        {
            get { return _isItemEditingMode; }
            set
            {
                _isItemEditingMode = value;
                OnPropertyChanged("IsItemEditMode");
            }
        }

        public bool IsAddingNewItem
        {
            get { return _isAddingNewItem; }
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
                foreach (var action in actions)
                {
                    action.UserName = Cache.Users.FirstOrDefault(i => i.id == action.patronid)?.DisplayNameTitle;
                    action.ItemName = Cache.Items.FirstOrDefault(i => i.id == action.itemid)?.title;
                    action.ActionType = action.action_type1.verb;
                }
                return actions;
            }
        }

        public List<item> ItemsShouldReturn
        {
            get
            {
                var items = Cache.Items.Where(i => i.due_date < DateTime.Today).OrderByDescending(i => i.due_date).Take(20).ToList();
                foreach (var item in items)
                {
                    item.Borrower = Cache.Users.FirstOrDefault(i => i.id == item.patronid);
                }
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

        private static bool AreSameUser(patron source, patron target)
        {
            return source.DisplayNameCh == target.DisplayNameCh &&
                   source.DisplayNameEn == target.DisplayNameEn &&
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
                   source.Borrower == target.Borrower &&
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

        private static bool IsValidUser(patron user)
        {
            return (!string.IsNullOrWhiteSpace(user.DisplayNameCh) || !string.IsNullOrWhiteSpace(user.DisplayNameEn)) && !string.IsNullOrWhiteSpace(user.barcode);
        }

        private static bool IsUserChanged(patron user)
        {
            if (user.id == -1)
            {
                return true;
            }
            using (var context = new LibraryDb())
            {
                var userInDb = context.patron.First(u => u.id == user.id);
                return !(userInDb.DisplayNameCh == user.DisplayNameCh &&
                         userInDb.DisplayNameEn == user.DisplayNameEn &&
                         userInDb.barcode == user.barcode &&
                         userInDb.picture.NullSequenceEqual(user.picture) &&
                         userInDb.limit == user.limit &&
                         userInDb.address == user.address &&
                         userInDb.phone == user.phone &&
                         userInDb.email == user.email &&
                         userInDb.created == user.created &&
                         userInDb.expiry == user.expiry);
            }
        }

        private static bool IsValidItem(item item)
        {
            return (!string.IsNullOrWhiteSpace(item.barcode) &&
                    !string.IsNullOrWhiteSpace(item.code) &&
                    !string.IsNullOrEmpty(item.title) &&
                    item.category != null &&
                    item.item_subclass != null &&
                    item.status != null);
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
                         itemInDb.Borrower == item.Borrower &&
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

        private async void AddActionHistory(LibraryDb context, int userId, int itemId, action_type.ActionTypeEnum actionType)
        {
            context.action_history.Add(new action_history
            {
                patronid = userId,
                itemid = itemId,
                action_type = (int)actionType,
                action_datetime = DateTime.Now
            });
            await Cache.RefreshMainCache();
            RefreshGridSource(DgActionHistory);
            RefreshGridSource(DgItemsShouldReturn);
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
                Application.Current.Shutdown();
                return;
            }
            boot.ShowDialog();
            if (!boot.AppPreparationSuccessfully)
            {
                Application.Current.Shutdown();
                return;
            }
            InitializeComponent();
            DataContext = this;
            Activate();
            // focus search box in the first run
            TbSearch.Focus();
        }

        //Tab selection change event, disable user/item editing
        private void OnTabSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!(e.OriginalSource is TabControl))
            {
                return;
            }
            if (((TabItem)e.AddedItems[0]).Name == "TabPeople" && ((TabItem)e.RemovedItems[0]).Name == "TabItem")
            {
                SelectedUser = (patron)DgUserGrid.SelectedItem;
            }
            if (((TabItem)e.AddedItems[0]).Name == "TabItem" && ((TabItem)e.RemovedItems[0]).Name == "TabPeople")
            {
                SelectedItem = (item)DgItemGrid.SelectedItem;
            }
            IsUserEditMode = false;
            IsItemEditMode = false;
            _isAddingNewUser = false;
            IsAddingNewItem = false;
        }

        /// <summary>
        /// Refresh user/item grid data source
        /// </summary>
        /// <param name="targetGrid"></param>
        private void RefreshGridSource(ItemsControl targetGrid)
        {
            switch (targetGrid.Name)
            {
                case "DgUserGrid":
                    targetGrid.ItemsSource = null;
                    targetGrid.ItemsSource = UserList;
                    break;
                case "DgItemGrid":
                    targetGrid.ItemsSource = null;
                    targetGrid.ItemsSource = ItemList;
                    break;
                case "DgActionHistory":
                    targetGrid.ItemsSource = null;
                    targetGrid.ItemsSource = ActionHistories;
                    ScrollGridToIndex((DataGrid)targetGrid, 0);
                    break;
                case "DgItemsShouldReturn":
                    targetGrid.ItemsSource = null;
                    targetGrid.ItemsSource = ItemsShouldReturn;
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
            _blurWorker.ApplyEffect(this);
            var appSettingWindow = new AppSettingWindow();
            appSettingWindow.ShowDialog();
            _blurWorker.ClearEffect(this);
            Activate();
        }

        //Search buttom click event
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
            var searchResults = new List<SearchResult>();
            try
            {
                //await Task.Delay(100);
                await Task.Run(() =>
                {
                    using (var context = new LibraryDb())
                    {
                        var userSearchResult = from user in context.patron
                                               where user.id.ToString().Contains(searchString) ||
                                                     user.firstnames_en.Contains(searchString) ||
                                                     user.firstnames_ch.Contains(searchString) ||
                                                     user.surname_en.Contains(searchString) ||
                                                     user.surname_ch.Contains(searchString)
                                               select user;
                        foreach (var result in userSearchResult)
                        {
                            var temp = new SearchResult
                            {
                                RecordType = SearchResultTypes.User,
                                UserId = result.id,
                                FirstNameCh = result.firstnames_ch,
                                LastNameCh = result.surname_ch,
                                FirstNameEn = result.firstnames_en,
                                LastNameEn = result.surname_en,
                                Img = (result.picture == null || result.picture.Length == 0) ? GetImageBytes(_currentDirectoryPath + NoPictureImgPath) : result.picture
                            };
                            if (!string.IsNullOrWhiteSpace(result.DisplayNameCh))
                            {
                                temp.FirstDisplayInfo = result.DisplayNameCh;
                            }
                            if (!string.IsNullOrWhiteSpace(result.DisplayNameEn))
                            {
                                temp.SecondDisplayInfo = result.DisplayNameEn;
                            }
                            searchResults.Add(temp);
                        }

                        var itemSearchResult = from item in context.item
                                               where item.id.ToString().Contains(searchString) ||
                                                     item.title.Contains(searchString) ||
                                                     item.isbn.Contains(searchString) ||
                                                     item.barcode.Contains(searchString)
                                               select item;
                        foreach (var result in itemSearchResult)
                        {
                            var temp = new SearchResult
                            {
                                RecordType = SearchResultTypes.Item,
                                Img = GetImageBytes(GetItemImgPath(result.item_subclass)),
                                ItemId = result.id,
                                Title = result.title,
                                Barcode = result.barcode,
                                Isbn = result.isbn
                            };
                            temp.FirstDisplayInfo = temp.Title;
                            temp.SecondDisplayInfo = $"ID: {temp.ItemId} Barcode: {temp.Barcode}";
                            searchResults.Add(temp);
                        }
                    }
                });
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
                        case SearchResultTypes.User:
                            int userId = searchResults[0].UserId;
                            TabUser.IsSelected = true;
                            ScrollGridToIndex(DgUserGrid, UserList.FindIndex(user => user.id == userId));
                            break;
                        case SearchResultTypes.Item:
                            int itemId = searchResults[0].ItemId;
                            TabItem.IsSelected = true;
                            ScrollGridToIndex(DgItemGrid, ItemList.FindIndex(item => item.id == itemId));
                            break;
                    }
                }
                else
                {
                    _blurWorker.ApplyEffect(this);
                    try
                    {
                        var searchResultWindow = new SearchResultWindow(searchResults);
                        searchResultWindow.ShowDialog();
                        _blurWorker.ClearEffect(this);
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

        //Navigate to user detail from history
        private async void BtnHistoryNavToUser_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var history = ((Rectangle)sender).DataContext as action_history;
                if (history == null)
                {
                    throw new InvalidCastException("Cast -> null value.");
                }
                int index = UserList.FindIndex(user => user.id == history.patronid);
                if (index == -1)
                {
                    throw new EntryPointNotFoundException("Unable to find user.");
                }
                TabUser.IsSelected = true;
                ScrollGridToIndex(DgUserGrid, index);
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Error on Navigating to User");
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
        private void HistoryGridRefreshIconClick(object sender, MouseButtonEventArgs e)
        {
            RefreshGridSource(DgActionHistory);
            ScrollGridToIndex(DgActionHistory, 0);
        }

        //Refresh items should return
        private void ItemsShouldReturnGridRefreshIconClick(object sender, MouseButtonEventArgs e)
        {
            RefreshGridSource(DgItemsShouldReturn);
            ScrollGridToIndex(DgItemsShouldReturn, 0);
        }

        //Navigate to user detail
        private async void BtnItemShouldReturnNavToUser_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                var record = ((Rectangle)sender).DataContext as item;
                if (record == null)
                {
                    throw new InvalidCastException("Cast -> null value.");
                }
                int index = UserList.FindIndex(user => user.id == record.patronid);
                if (index == -1)
                {
                    throw new EntryPointNotFoundException("Unable to find user.");
                }
                TabUser.IsSelected = true;
                ScrollGridToIndex(DgUserGrid, index);
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Error on Navigating to user");
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

        private void NavigateUserTabIconClick(object sender, MouseButtonEventArgs e)
        {
            TabUser.IsSelected = true;
        }

        private void NavigateItemTabIconClick(object sender, MouseButtonEventArgs e)
        {
            TabItem.IsSelected = true;
        }

        private void OpenAppSettingsIconClick(object sender, MouseButtonEventArgs e)
        {
            OpenAppSettingsWindow();
        }

        #endregion

        #region User Tab Logics

        /// <summary>
        /// Save user to database
        /// </summary>
        private async void SaveUserHandler()
        {
            if (_isAddingNewUser)
            {
                try
                {
                    using (var context = new LibraryDb())
                    {
                        using (var transactionScope = new TransactionScope())
                        {
                            //might be can use context.patron.AddOrUpdate();
                            if (SelectedUser.id == -1)
                            {
                                if (IsValidUser(SelectedUser))
                                {
                                    context.patron.Add(SelectedUser);
                                }
                                else
                                {
                                    await this.ShowMessageAsync("Info", "Please provide valid Name and Barcode.");
                                    return;
                                }
                            }
                            else
                            {
                                throw new InvalidDataException("Invalid user.");
                            }
                            context.SaveChanges();
                            transactionScope.Complete();
                        }
                    }
                    await Cache.RefreshMainCache();
                    RefreshGridSource(DgItemsShouldReturn);
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Error on adding new user.");
                    await this.ShowMessageAsync("Error", "Error on adding new user to the database, please contact administrator.");
                    IsUserEditMode = false;
                    RefreshGridSource(DgUserGrid);
                    ScrollGridToIndex(DgUserGrid, 0);

                    return;
                }
                RefreshGridSource(DgUserGrid);
                ScrollGridToIndex(DgUserGrid, DgUserGrid.Items.Count - 1);
                IsUserEditMode = false;
                _isAddingNewUser = false;
            }
            else
            {
                if (IsUserChanged(SelectedUser))
                {
                    try
                    {
                        using (var context = new LibraryDb())
                        {
                            using (var transactionScope = new TransactionScope())
                            {
                                var userInDb = context.patron.FirstOrDefault(user => user.id == SelectedUser.id);
                                if (userInDb == null)
                                {
                                    throw new EntryPointNotFoundException("Unable to find selected user.");
                                }
                                context.Entry(userInDb).CurrentValues.SetValues(SelectedUser);
                                context.SaveChanges();
                                transactionScope.Complete();
                            }
                        }
                        await Cache.RefreshMainCache();
                        RefreshGridSource(DgItemsShouldReturn);
                    }
                    catch (Exception exception)
                    {
                        Logger.Error(exception, "Error on Saving user.");
                        await this.ShowMessageAsync("Error", exception.Message);
                        ScrollGridToIndex(DgUserGrid, 0);
                        return;
                    }
                    IsUserEditMode = false;
                    OnPropertyChanged("SelectedUser");
                }
                else
                {
                    IsUserEditMode = false;
                }
            }
        }

        /// <summary>
        /// Handle unsaved user
        /// </summary>
        private async void UnsavedUserHandler()
        {
            if (IsUserChanged(SelectedUser))
            {
                var result = await UnsavedDialog();
                if (result != MessageDialogResult.Affirmative)
                {
                    IsUserEditMode = true;
                    return;
                }
            }
            //cancel saving, restore last selection
            if (SelectedUser.id == -1)
            {
                ScrollGridToIndex(DgUserGrid, 0);
            }
            else
            {
                SelectedUser = DgUserGrid.Items.OfType<patron>().First(i => i.id == SelectedUser.id);
            }
            _isAddingNewUser = false;
            IsUserEditMode = false;
            OnPropertyChanged("SelectedUser");
        }

        //User filter text change event
        private void UserFilterChanged(object sender, TextChangedEventArgs e)
        {
            _userFilter = TbUserFilter.Text;
            OnPropertyChanged("UserList");
            SelectedUser = (patron)DgUserGrid.SelectedItem;
        }

        //Refresh user grid
        private void UserGridRefreshIconClick(object sender, MouseButtonEventArgs e)
        {
            RefreshGridSource(DgUserGrid);
            ScrollGridToIndex(DgUserGrid, 0);
        }

        //User click on data grid
        private async void UserGridPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (IsUserEditMode)
            {
                if (IsUserChanged(SelectedUser))
                {
                    e.Handled = true;
                    await this.ShowMessageAsync("Warning", "Please save your changes before navigating to another user.");
                    return;
                }
                IsUserEditMode = false;
            }
            e.Handled = false;
        }

        //User use keyboard to navigate on data grid
        private async void UserGridPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (IsUserEditMode)
            {
                if (IsUserChanged(SelectedUser))
                {
                    e.Handled = true;
                    await this.ShowMessageAsync("Warning", "Please save your changes before navigating to another user.");
                    return;
                }
                IsUserEditMode = false;
            }
            e.Handled = false;
        }

        //User grid selection change event
        private void UserGridSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedUser = (patron)DgUserGrid.SelectedItem;
        }

        //Add new user
        private void AddUserIconClick(object sender, MouseButtonEventArgs e)
        {
            if (_isAddingNewUser)
            {
                return;
            }
            DgUserGrid.SelectedItem = null;
            var newUser = new patron
            {
                id = -1,
                limit = BusinessRules.DefaultLimitPerUser,
                created = DateTime.Today,
                expiry = DateTime.Today.AddYears(1)
            };
            SelectedUser = newUser;
            IsUserEditMode = true;
            _isAddingNewUser = true;
        }

        //Edit mode icon click
        private void UserEditModeIconClick(object sender, MouseButtonEventArgs e)
        {
            if (IsUserEditMode)
            {
                UnsavedUserHandler();
            }
            else
            {
                IsUserEditMode = true;
            }
        }

        //Edit mode toggle icon click
        private void ToggleUserEditMode_Click(object sender, RoutedEventArgs e)
        {
            if (IsUserEditMode)
            {
                UnsavedUserHandler();
            }
            else
            {
                IsUserEditMode = true;
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
        private async void BtnUserRenewItem_Click(object sender, RoutedEventArgs routedEventArgs)
        {
            try
            {
                var tryToRenewItem = ((Rectangle)sender).DataContext as item;
                int selectIndex = SelectedUser.BorrowingItems.IndexOf(tryToRenewItem);
                if (tryToRenewItem == null)
                {
                    throw new InvalidCastException("Cast -> null value.");
                }
                using (var context = new LibraryDb())
                {
                    using (var transactionScope = new TransactionScope())
                    {
                        var itemInDb = context.item.FirstOrDefault(item => item.id == tryToRenewItem.id);
                        if (itemInDb == null)
                        {
                            throw new EntryPointNotFoundException("Unable to find selected item.");
                        }
                        itemInDb.due_date = DateTime.Today.AddDays(BusinessRules.RenewPeriodInDay);
                        AddActionHistory(context, SelectedUser.id, itemInDb.id, action_type.ActionTypeEnum.Renew);
                        context.SaveChanges();
                        SelectedUser.BorrowingItems = context.item.Where(item => item.patronid == SelectedUser.id).ToList();
                        transactionScope.Complete();
                        DgCurrentBorrowingItem.Items.Refresh();
                    }
                }
                OnPropertyChanged("SelectedUser");
                DgCurrentBorrowingItem.SelectedIndex = SelectedUser.BorrowingItems.Count > selectIndex ? selectIndex : SelectedUser.BorrowingItems.Count - 1;
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Error on Renewing item");
                await this.ShowMessageAsync("Error", exception.Message);
            }
        }

        //Return selected borrowed item
        private async void BtnReturnItem_Click(object sender, RoutedEventArgs routedEventArgs)
        {
            try
            {
                var tryToReturnItem = ((Rectangle)sender).DataContext as item;
                int selectIndex = SelectedUser.BorrowingItems.IndexOf(tryToReturnItem);
                if (tryToReturnItem == null)
                {
                    throw new InvalidCastException("Cast -> null value.");
                }
                using (var context = new LibraryDb())
                {
                    using (var transactionScope = new TransactionScope())
                    {
                        var itemInDb = context.item.FirstOrDefault(item => item.id == tryToReturnItem.id);
                        if (itemInDb == null)
                        {
                            throw new EntryPointNotFoundException("Unable to find selected item.");
                        }
                        itemInDb.patronid = null;
                        AddActionHistory(context, SelectedUser.id, itemInDb.id, action_type.ActionTypeEnum.Return);
                        context.SaveChanges();
                        SelectedUser.BorrowingItems = context.item.Where(item => item.patronid == SelectedUser.id).ToList();
                        transactionScope.Complete();
                        DgCurrentBorrowingItem.Items.Refresh();
                    }
                }
                OnPropertyChanged("SelectedUser");
                DgCurrentBorrowingItem.SelectedIndex = SelectedUser.BorrowingItems.Count > selectIndex ? selectIndex : SelectedUser.BorrowingItems.Count - 1;
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Error on Returning item");
                await this.ShowMessageAsync("Error", exception.Message);
            }
        }

        //Borrow item to selected user
        private async void BtnUserBorrowItem_Click(object sender, RoutedEventArgs e)
        {
            string dialogResult = await this.ShowInputAsync("Borrow Item", "Please enter item Barcode");
            if (string.IsNullOrWhiteSpace(dialogResult))
            {
                return;
            }
            int queryId;
            if (!int.TryParse(dialogResult, out queryId))
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
                    var currenBorrower = context.patron.FirstOrDefault(user => user.id == itemInDb.patronid);
                    if (currenBorrower == null)
                    {
                        using (var transactionScope = new TransactionScope())
                        {
                            itemInDb.patronid = SelectedUser.id;
                            itemInDb.due_date = DateTime.Today.AddDays(BusinessRules.RenewPeriodInDay);
                            SelectedUser.BorrowingItems.Add(itemInDb);
                            AddActionHistory(context, SelectedUser.id, itemInDb.id, action_type.ActionTypeEnum.Lend);
                            context.SaveChanges();
                            transactionScope.Complete();
                        }
                        DgCurrentBorrowingItem.Items.Refresh();
                        OnPropertyChanged("SelectedUser");
                    }
                    else
                    {
                        var result = await NavigateDialog($"{currenBorrower.DisplayNameTitle} is holding this item currently, click OK navigate to item detail.");
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
        }

        //Update user
        private void BtnSaveUser_Click(object sender, RoutedEventArgs e)
        {
            SaveUserHandler();
        }

        //Renew User
        private async void BtnRenewUser_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedUser.id == -1)
            {
                return;
            }
            try
            {
                using (var context = new LibraryDb())
                {
                    using (var transactionScope = new TransactionScope())
                    {
                        var userInDb = context.patron.FirstOrDefault(user => user.id == SelectedUser.id);
                        if (userInDb == null)
                        {
                            throw new EntryPointNotFoundException("Unable to find selected user.");
                        }
                        userInDb.expiry = DateTime.Today.AddYears(1);
                        context.SaveChanges();
                        transactionScope.Complete();
                    }
                }
                SelectedUser = UserList.First(i => i.id == SelectedUser.id);
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Error on Renewing user.");
                await this.ShowMessageAsync("Error", exception.Message);
            }
        }

        //Cancle user editing
        private void BtnCancelEditingUser_Click(object sender, RoutedEventArgs e)
        {
            UnsavedUserHandler();
        }

        //Delete User
        private async void BtnDeleteUser_Click(object sender, RoutedEventArgs e)
        {
            if (_isAddingNewUser)
            {
                var result = await UnsavedDialog();
                if (result == MessageDialogResult.Affirmative)
                {
                    IsUserEditMode = false;
                    _isAddingNewUser = false;
                    RefreshGridSource(DgUserGrid);
                    ScrollGridToIndex(DgUserGrid, 0);
                }
                else
                {
                    IsUserEditMode = true;
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
                            var userInDb = context.patron.FirstOrDefault(user => user.id == SelectedUser.id);
                            if (userInDb == null)
                            {
                                throw new EntryPointNotFoundException("Unable to find selected user.");
                            }
                            if (!userInDb.AllowToDelete)
                            {
                                throw new InvalidOperationException("Cannot delete this user.");
                            }
                            var result = await this.ShowMessageAsync("Delete Confirmation",
                                                                     $"Are you sure you want to DELETE this user?{Environment.NewLine}This CANNOT be undone!",
                                                                     MessageDialogStyle.AffirmativeAndNegative,
                                                                     _binarySelectionDialogSettings);
                            if (result != MessageDialogResult.Affirmative)
                            {
                                return;
                            }
                            if (context.item.Any(item => item.patronid == userInDb.id))
                            {
                                await this.ShowMessageAsync("Warning", "Cannot delete this use, because this user still holding some books/items from library.");
                                return;
                            }
                            context.patron.Where(user => user.id == SelectedUser.id).Delete();
                            context.SaveChanges();
                            transactionScope.Complete();
                        }
                    }
                    IsUserEditMode = false;
                    RefreshGridSource(DgUserGrid);
                    ScrollGridToIndex(DgUserGrid, 0);
                }
                catch (Exception exception)
                {
                    Logger.Error(exception, "Error on Deleting user.");
                    await this.ShowMessageAsync("Error", exception.Message);
                    RefreshGridSource(DgUserGrid);
                    ScrollGridToIndex(DgUserGrid, 0);
                }
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
            SelectedUser.picture = GetImageBytes(filePath);
            OnPropertyChanged("SelectedUser");
        }

        //Delete current user photo
        private void DeleteUserPhotoClick(object sender, MouseButtonEventArgs e)
        {
            SelectedUser.picture = null;
            OnPropertyChanged("SelectedUser");
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
                    await Cache.RefreshMainCache();
                    RefreshGridSource(DgItemsShouldReturn);
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Error on adding new item.");
                    await this.ShowMessageAsync("Error", "Error on adding new item to the database, please contact administrator.");
                    RefreshGridSource(DgItemGrid);
                    ScrollGridToIndex(DgItemGrid, 0);
                    return;
                }
                RefreshGridSource(DgItemGrid);
                IsItemEditMode = false;
                IsAddingNewItem = false;
                ScrollGridToIndex(DgItemGrid, DgItemGrid.Items.Count - 1);
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
                        await Cache.RefreshMainCache();
                        RefreshGridSource(DgItemsShouldReturn);
                    }
                    catch (Exception exception)
                    {
                        Logger.Error(exception, "Error on Saving item.");
                        await this.ShowMessageAsync("Error", exception.Message);
                        ScrollGridToIndex(DgItemGrid, 0);
                        return;
                    }
                    IsItemEditMode = false;
                    OnPropertyChanged("SelectedItem");
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
                SelectedItem = DgItemGrid.Items.OfType<item>().First(i => i.id == SelectedItem.id);
            }
            IsAddingNewItem = false;
            IsItemEditMode = false;
            OnPropertyChanged("SelectedItem");
        }

        //Item filter text change event
        private void ItemFilterChanged(object sender, TextChangedEventArgs e)
        {
            _itemFilter = TbItemFilter.Text;
            OnPropertyChanged("ItemList");
            SelectedItem = (item)DgItemGrid.SelectedItem;
        }

        //Refresh item grid
        private void ItemGridRefreshIconClick(object sender, MouseButtonEventArgs e)
        {
            RefreshGridSource(DgItemGrid);
            ScrollGridToIndex(DgItemGrid, 0);
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

        //Issue an item to user
        private async void BtnIssueToUser_Click(object sender, RoutedEventArgs e)
        {
            string enteredUserBarcode = TbIssueToUserBarcode.Text.Trim();
            if (string.IsNullOrWhiteSpace(enteredUserBarcode))
            {
                return;
            }
            try
            {
                using (var context = new LibraryDb())
                {
                    var itemInDb = context.item.First(i => i.id == SelectedItem.id);
                    var userInDb = context.patron.FirstOrDefault(i => i.barcode.Equals(enteredUserBarcode, StringComparison.InvariantCultureIgnoreCase));
                    if (userInDb == null)
                    {
                        throw new EntryPointNotFoundException("Unable to find item in database.");
                    }
                    if (!userInDb.CanBorrowItem)
                    {
                        var result = await NavigateDialog($"{userInDb.DisplayNameTitle} Cannot borrow more items, click Ok navigate to user details.");
                        if (result != MessageDialogResult.Affirmative)
                        {
                            return;
                        }
                        TabUser.IsSelected = true;
                        int index = UserList.FindIndex(i => i.id == userInDb.id);
                        if (index == -1)
                        {
                            throw new EntryPointNotFoundException("Unable to find item.");
                        }
                        ScrollGridToIndex(DgUserGrid, index);
                        return;
                    }
                    using (var transactionScope = new TransactionScope())
                    {
                        itemInDb.patronid = userInDb.id;
                        itemInDb.due_date = DateTime.Today.AddDays(BusinessRules.RenewPeriodInDay);
                        AddActionHistory(context, userInDb.id, itemInDb.id, action_type.ActionTypeEnum.Lend);
                        context.SaveChanges();
                        transactionScope.Complete();
                    }
                    SelectedItem = itemInDb;
                    TbIssueToUserBarcode.Text = string.Empty;
                }
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Error on Issuing item to user.");
                await this.ShowMessageAsync("Error", exception.Message);
            }

        }

        //Navigate to view user detail
        private async void BtnViewUserDetail_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int index = UserList.FindIndex(user => user.id == SelectedItem.Borrower.id);
                if (index == -1)
                {
                    throw new EntryPointNotFoundException("Unable to find borrower.");
                }
                TabUser.IsSelected = true;
                ScrollGridToIndex(DgUserGrid, index);
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Error on Viewing item borrower.");
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
                        itemInDb.due_date = DateTime.Today.AddDays(BusinessRules.RenewPeriodInDay);
                        AddActionHistory(context, SelectedItem.Borrower.id, itemInDb.id, action_type.ActionTypeEnum.Renew);
                        context.SaveChanges();
                        transactionScope.Complete();
                    }
                }
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Error on Renewing item.");
                await this.ShowMessageAsync("Error", exception.Message);
            }
        }

        //Return current item
        private async void BtnReturnBook_Click(object sender, RoutedEventArgs e)
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
                        itemInDb.patronid = null;
                        AddActionHistory(context, SelectedItem.Borrower.id, itemInDb.id, action_type.ActionTypeEnum.Return);
                        context.SaveChanges();
                        transactionScope.Complete();
                    }
                }
                SelectedItem = (item)DgItemGrid.SelectedItem;
            }
            catch (Exception exception)
            {
                Logger.Error(exception, "Error on Returning item.");
                await this.ShowMessageAsync("Error", exception.Message);
            }
        }

        //Update item
        private void BtnSaveItem_Click(object sender, RoutedEventArgs e)
        {
            SaveItemHandler();
        }

        //Cancle item editing
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
                    RefreshGridSource(DgItemGrid);
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
                    RefreshGridSource(DgItemGrid);
                    ScrollGridToIndex(DgItemGrid, 0);
                }
                catch (Exception exception)
                {
                    Logger.Error(exception, "Error on Deleting item.");
                    await this.ShowMessageAsync("Error", exception.Message);
                    RefreshGridSource(DgItemGrid);
                    ScrollGridToIndex(DgItemGrid, 0);
                }
            }
        }

        #endregion

        #endregion
    }
}