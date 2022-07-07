﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

using FlyleafLib;
using FlyleafLib.Controls.WPF;
using FlyleafLib.MediaPlayer;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
namespace FlyleafMultiPlayer__WPF_
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public Player PlayerView1 { get => _PlayerView1; set { _PlayerView1 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PlayerView1))); } }
        Player _PlayerView1;
        public Player PlayerView2 { get => _PlayerView2; set { _PlayerView2 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PlayerView2))); } }
        Player _PlayerView2;
        public Player PlayerView3 { get => _PlayerView3; set { _PlayerView3 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PlayerView3))); } }
        Player _PlayerView3;
        public Player PlayerView4 { get => _PlayerView4; set { _PlayerView4 = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PlayerView4))); } }
        Player _PlayerView4;

        public event PropertyChangedEventHandler? PropertyChanged;

        public ICommand RotatePlayers { get; set; }

        List<Player> Players = new List<Player>();

        public MainWindow()
        {
            // Initializes Engine (Specifies FFmpeg libraries path which is required)
            Engine.Start(new EngineConfig()
            {
                #if DEBUG
                LogOutput       = ":debug",
                LogLevel        = LogLevel.Debug,
                FFmpegLogLevel  = FFmpegLogLevel.Warning,
                #endif
                
                PluginsPath     = ":Plugins",
                FFmpegPath      = ":FFmpeg",
            });

            // Creates 4 Players and adds them in the PlayerViews
            for (int i=0; i<4; i++)
            {
                // Use performance wise config for multiple players
                var config = new Config();

                config.Demuxer.BufferDuration = TimeSpan.FromSeconds(5).Ticks; // Reduces RAM as the demuxer will not buffer large number of packets
                config.Decoder.MaxVideoFrames = 2; // Reduces VRAM as video decoder will not keep large queues in VRAM (should be tested for smooth video playback, especially for 4K)
                config.Decoder.VideoThreads = 2; // Reduces VRAM/GPU (should be tested for smooth video playback, especially for 4K)
                // Consider using lower quality streams on normal screen and higher quality on fullscreen (if available)

                Players.Add(new Player(config));
            }

            PlayerView1 = Players[0];
            PlayerView2 = Players[1];
            PlayerView3 = Players[2];
            PlayerView4 = Players[3];

            RotatePlayers = new RelayCommand(RotatePlayersAction);

            DataContext = this;

            InitializeComponent();
        }

        private void RotatePlayersAction(object obj)
        {
            // Clockwise rotation

            // User should review and possible unsubscribe from player/control events

            // Can use Player on VideoView1 as temporary
            Player.SwapPlayers(VideoView1.Player, VideoView2.Player);
            Player.SwapPlayers(VideoView1.Player, VideoView3.Player);
            Player.SwapPlayers(VideoView1.Player, VideoView4.Player);

            // User should review and possible re-subscribe from player/control events
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            MultiPlayer.Children.Remove(VideoView1);
            FullScreenWindow fullScreenWindow = new FullScreenWindow();
            fullScreenWindow.FullGrid.Children.Add(VideoView1);
            fullScreenWindow.ShowDialog();
        }
    }
}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
