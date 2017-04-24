using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using MahApps.Metro.IconPacks;

namespace ACMC_Library_System.UI
{
    public partial class NotificationBar
    {
        private const string DefaultIconColor = "#FFF";
        private const string DefaultMsgColor = "#FFF";

        public static readonly RoutedEvent ShowEvent = EventManager.RegisterRoutedEvent("Show", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(NotificationBar));

        public event RoutedEventHandler Show
        {
            add => AddHandler(ShowEvent, value);
            remove => RemoveHandler(ShowEvent, value);
        }

        private void RaiseShowEvent()
        {
            var newEventArgs = new RoutedEventArgs(ShowEvent);
            RaiseEvent(newEventArgs);
        }

        /// <summary>
        /// Show Notification bar
        /// </summary>
        /// <param name="icon">Icon on notification bar</param>
        /// <param name="iconColorHex">Icon filling color hax</param>
        /// <param name="msg">Message shows on notification</param>
        /// <param name="msgColorHex">Message color hax</param>
        /// <param name="backgroundColorHex">Background color hax</param>
        /// <param name="autoCloseInSec">Notification disappear in second</param>
        private void ShowNotification(Visual icon, string iconColorHex, string msg, string msgColorHex, string backgroundColorHex, int autoCloseInSec)
        {
            NotificationMsg.Text = msg;
            NotificationMsg.Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom(msgColorHex);
            NotificationIcon.OpacityMask = new VisualBrush(icon);
            NotificationIcon.Fill = (SolidColorBrush)new BrushConverter().ConvertFrom(iconColorHex);
            NotificationGrid.Background = (SolidColorBrush)new BrushConverter().ConvertFrom(backgroundColorHex);
            NotificationGrid.Visibility = Visibility.Visible;
            ExitInitial.KeyTime = new TimeSpan(0, 0, autoCloseInSec == 0 ? 0 : autoCloseInSec - 1);
            ExitFinal.KeyTime = new TimeSpan(0, 0, autoCloseInSec);
            RaiseShowEvent();
        }

        /// <summary>
        /// Remove a Notification bar if one is currently being shown.
        /// </summary>
        private void ClearNotification()
        {
            NotificationGrid.Visibility = Visibility.Collapsed;
            NotificationMsg.Text = string.Empty;
            NotificationIcon.OpacityMask = null;
        }

        public NotificationBar()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Shows an Information Notification
        /// </summary>
        /// <param name="message">The message for the notification</param>
        /// <param name="autoCloseInSec">Notification will auto-close in this amount of seconds</param>
        public void Infomation(string message, int autoCloseInSec = 0)
        {
            const string BackgroundColor = "#5BC0DE";
            var icon = new PackIconMaterial
            {
                Kind = PackIconMaterialKind.InformationOutline,
                VerticalAlignment = VerticalAlignment.Center
            };
            ShowNotification(icon, DefaultIconColor, message, DefaultMsgColor, BackgroundColor, autoCloseInSec);
        }

        /// <summary>
        /// Shows a Success Notification
        /// </summary>
        /// <param name="message">The message for the notification</param>
        /// <param name="autoCloseInSec">Notification will auto-close in this amount of seconds</param>
        public void Success(string message, int autoCloseInSec = 0)
        {
            const string BackgroundColor = "#5EB95E";
            var icon = new PackIconMaterial
            {
                Kind = PackIconMaterialKind.AlertBox,
                VerticalAlignment = VerticalAlignment.Center
            };
            ShowNotification(icon, DefaultIconColor, message, DefaultMsgColor, BackgroundColor, autoCloseInSec);
        }

        /// <summary>
        /// Shows a warning Notification
        /// </summary>
        /// <param name="message">The message for the notification</param>
        /// <param name="autoCloseInSec">Notification will auto-close in this amount of seconds</param>
        public void Warning(string message, int autoCloseInSec = 0)
        {
            const string BackgroundColor = "#FF9616";
            var icon = new PackIconMaterial
            {
                Kind = PackIconMaterialKind.AlertBox,
                VerticalAlignment = VerticalAlignment.Center
            };
            ShowNotification(icon, DefaultIconColor, message, DefaultMsgColor, BackgroundColor, autoCloseInSec);
        }

        /// <summary>
        /// Shows a Danger Notification
        /// </summary>
        /// <param name="message">The message for the notification</param>
        /// <param name="autoCloseInSec">Notification will auto-close in this amount of seconds</param>
        public void Error(string message, int autoCloseInSec = 0)
        {
            const string BackgroundColor = "#FFE74C3C";
            var icon = new PackIconMaterial
            {
                Kind = PackIconMaterialKind.AlertBox,
                VerticalAlignment = VerticalAlignment.Center
            };
            ShowNotification(icon, DefaultIconColor, message, DefaultMsgColor, BackgroundColor, autoCloseInSec);
        }

        /// <summary>
        /// Create a custom Notification bar
        /// </summary>
        /// <param name="icon">Icon on notification bar</param>
        /// <param name="iconColorHex">Icon filling color hax</param>
        /// <param name="message">Message shows on notification</param>
        /// <param name="messageColorHex">Message color hax</param>
        /// <param name="backgroundColorHex">Background color hax</param>
        /// <param name="disappearInSec">Notification disappear in second</param>
        public void Create(Visual icon, string iconColorHex, string message, string messageColorHex, string backgroundColorHex, int disappearInSec)
        {
            ShowNotification(icon, iconColorHex, message, messageColorHex, backgroundColorHex, disappearInSec);
        }

        private void NotificationClick(object sender, MouseButtonEventArgs e)
        {
            ClearNotification();
        }

        private void AnimationCompleted(object sender, EventArgs e)
        {
            if (!(NotificationGrid.Opacity < 0.01))
            {
                return;
            }
            //When calling ShowNotification() the window is not rendered yet.  
            //So opacity is 0. If you have a timeout of 0 then it would call this immediately
            if (ExitInitial.KeyTime.TimeSpan.Seconds > 0)
            {
                ClearNotification();
            }
        }
    }
}
