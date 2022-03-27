﻿using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;

using FlyleafLib;
using FlyleafLib.MediaPlayer;

namespace FlyleafPlayer
{
    /// <summary>
    /// FlyleafPlayer (WPF Control) Sample
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Flyleaf Player binded to VideoView
        /// </summary>
        public Player Player { get; set; }
        public Config Config { get; set; }

        public MainWindow()
        {
            // detect command line input
            string[] arguments = Environment.GetCommandLineArgs();
            if (arguments.Length > 1 && arguments[1] != String.Empty)
            {
                // set working directory to installed dir, so the app can reference external libraries
                Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            }

            // Ensures that we have enough worker threads to avoid the UI from freezing or not updating on time
            ThreadPool.GetMinThreads(out int workers, out int ports);
            ThreadPool.SetMinThreads(workers + 6, ports + 6);

            // Initializes Engine (Specifies FFmpeg libraries path which is required)
            Engine.Start(new EngineConfig()
            {
                #if DEBUG
                LogOutput           = ":debug",
                LogLevel            = LogLevel.Debug,
                FFmpegLogLevel      = FFmpegLogLevel.Warning,
                #endif
                
                PluginsPath         = ":Plugins",
                FFmpegPath          = ":FFmpeg",
                HighPerformaceTimers= false // Less power consumption (safe to use only for single player)
            });

            Config = new Config();

            #if RELEASE
            // Loads the configuration file before initializing the player (if exists)
            if (File.Exists("Flyleaf.Config.xml"))
                try { Config = Config.Load("Flyleaf.Config.xml"); } catch { }
            #endif

            // Initializes the Player
            Player = new Player(Config);

            // Allowing VideoView to access our Player
            DataContext = this;

            InitializeComponent();

            // Allow Flyleaf WPF Control to Load UIConfig and Save both Config & UIConfig (Save button will be available in settings)
            flyleafControl.ConfigPath = "Flyleaf.Config.xml";
            flyleafControl.UIConfigPath = "Flyleaf.UIConfig.xml";

            // If the user requests reverse playback allocate more frames once
            Player.PropertyChanged += (o, e) =>
            {
                if (e.PropertyName == "ReversePlayback" && !ReversePlaybackChecked)
                {
                    ReversePlaybackChecked = true;
                    if (Config.Decoder.MaxVideoFrames < 40)
                        Config.Decoder.MaxVideoFrames = 40;
                }
            };
        }
        bool ReversePlaybackChecked;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Gives access to keyboard events on start up
            Player.VideoView.WinFormsHost.Focus();

            #if RELEASE
            // Ensures that the Control's handle has been created and the renderer has been fully initialized (so we can save also the filters parsed by the library)
            if (!Config.Loaded)
            {
                try
                {
                    Utils.AddFirewallRule();
                    Config.Save("Flyleaf.Config.xml");
                } catch { }
            }
            #endif
        }

        #region Dark Theme
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            //Enable Dark Titlebar
            DwmApi.ToggleImmersiveDarkMode(new WindowInteropHelper(this).Handle, true);
        }

        public static class DwmApi
        {
            private const int S_OK = 0;

            // This two flags are not currently documented
            // and they might change in the future
            private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
            private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

            private const int WINDOWS10_MIN_BUILD_NUMBER = 17763;
            private const int WINDOWS10_20H1_BUILD_NUMBER = 18985;

            public static void ToggleImmersiveDarkMode(IntPtr window, bool enable)
            {
                if (!IsWindows10OrGreater(WINDOWS10_MIN_BUILD_NUMBER))
                {
                    // Dark mode is not supported
                    //_ = MessageBox.Show($"{Environment.OSVersion.Version.Build}not s");
                    return;
                }

                int useImmersiveDarkMode = enable ? 1 : 0;
                CheckHResult(DwmSetWindowAttribute(window, ImmersiveDarkModeAttribute, ref useImmersiveDarkMode, sizeof(int)));
            }

            [DllImport("dwmapi.dll", PreserveSig = true)]
            private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attributeValue, int attributeSize);

            private static bool IsWindows10OrGreater(int build)
            {
                return IsWindow10 && HasAtLeastBuildNumber(build);
            }

            private static bool IsWindow10
                => Environment.OSVersion.Version.Major >= 10;

            private static bool HasAtLeastBuildNumber(int build)
            {
                return Environment.OSVersion.Version.Build >= build;
            }

            private static int ImmersiveDarkModeAttribute
                => HasAtLeastBuildNumber(WINDOWS10_20H1_BUILD_NUMBER)
                    ? DWMWA_USE_IMMERSIVE_DARK_MODE
                    : DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1;

            private static void CheckHResult(int hResult)
            {
                if (hResult != S_OK)
                {
                    //throw new Win32Exception(hResult);
                    //_ = MessageBox.Show(new Win32Exception(hResult).Message);
                }
            }
        }
        #endregion
    }
}
