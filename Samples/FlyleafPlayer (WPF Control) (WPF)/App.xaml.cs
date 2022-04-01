using System;
using System.IO;
using System.Windows;

using UtilityClasses;

namespace FlyleafPlayer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        MainWindow mainWindow;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // set working directory to installed dir, so the app can reference external libraries
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

            DynamicArgument da = DynamicArgument.Create();
            da.ArgumentReceived += Application_ArgumentReceived;
            bool result = da.Start();
            if (!result) // a instance is already running, do not load resource to open new window
            {
                da.ArgumentReceived -= Application_ArgumentReceived;
                Environment.Exit(0);
                return;
            } else
            {
                // load Main Window, this shall be called first time only
                CreateMainWindow();
            }
        }

        private void CreateMainWindow()
        {
            lock (this)
            {
                if (mainWindow == null)
                {
                    mainWindow = new();
                    MainWindow = mainWindow;
                    mainWindow.Show();
                }
            }
        }

        private void Application_ArgumentReceived(object sender, DynamicArgument.DynamicArgmentEventArgs e)
        {
            if (e.Argument != null && e.Argument != String.Empty && File.Exists(e.Argument))
            {
                if (mainWindow == null) // when app is created from double clicking a media file, this event triggers before main window is created
                {
                    CreateMainWindow();
                }
                if (mainWindow.Player.VideoView == null)
                {
                    string playOnLoaded = e.Argument;
                    mainWindow.Loaded += (o, e) =>
                    {
                        mainWindow.Player.OpenAsync(playOnLoaded);
                    };
                }
                else
                {
                    mainWindow.Player.OpenAsync(e.Argument);
                }
            }
        }
    }
}
