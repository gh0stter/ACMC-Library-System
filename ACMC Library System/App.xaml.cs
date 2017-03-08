using System;
using System.Collections.Generic;
using System.Windows;
using DomainModels.DataModel;
using Microsoft.Shell;

namespace ACMC_Library_System
{
    /// <summary>
    ///     App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application, ISingleInstanceApp
    {
        //unique string to identify the application
        private const string Unique = "ACMC Lib";

        #region ISingleInstanceApp Members

        public bool SignalExternalCommandLineArgs(IList<string> args)
        {
            // Handle command line arguments of second instance
            // Bring window to foreground
            if (MainWindow.WindowState == WindowState.Minimized)
            {
                MainWindow.WindowState = WindowState.Normal;
            }
            MainWindow.Activate();
            return true;
        }

        #endregion

        [STAThread]
        public static void Main()
        {
            if (!SingleInstance<App>.InitializeAsFirstInstance(Unique))
            {
                return;
            }
            var application = new App();

            //ini business rules
            var appSettings = ACMC_Library_System.Properties.Settings.Default;
            BusinessRules.RenewPeriodInDay = appSettings.RenewPeriodInDay;
            BusinessRules.DefaultQuotaPerMember = appSettings.DefaultQuotaPerMember;
            BusinessRules.FinesPerWeek = appSettings.FinesPerWeek;
            BusinessRules.LibMemberBarcode = appSettings.LibMemberBarcode;
            BusinessRules.LibMemberId = appSettings.LibMemberId;

            application.InitializeComponent();
            application.Run();

            // Allow single instance code to perform cleanup operations
            SingleInstance<App>.Cleanup();
        }
    }
}