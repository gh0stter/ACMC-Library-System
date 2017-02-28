using System;
using System.Collections.Generic;
using System.Windows;
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
            //if (this.MainWindow.WindowState == WindowState.Minimized)
            //{
            //    this.MainWindow.WindowState = WindowState.Normal;
            //}
            MainWindow.Activate();
            return true;
        }

        #endregion

        [STAThread]
        public static void Main()
        {
            if (SingleInstance<App>.InitializeAsFirstInstance(Unique))
            {
                var application = new App();
                application.InitializeComponent();
                application.Run();

                // Allow single instance code to perform cleanup operations
                SingleInstance<App>.Cleanup();
            }
        }
    }
}