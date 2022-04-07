using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using FlyleafLib.MediaPlayer;
using MaterialDesignThemes.Wpf;

namespace FlyleafLib.Controls.WPF
{
    /// <summary>
    /// Interaction logic for PlaylistViewer.xaml
    /// </summary>
    public partial class PlaylistViewer : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// The item in playlist, and the current status (playing/non-playing)
        public struct Item
        {
            public Item(string name, bool isSelected)
            {
                Name = name;
                IsSelected = isSelected;
            }
            public string Name { get; }
            public bool IsSelected { get; }
        }

        public static readonly DependencyProperty IsOpenProp =
            DependencyProperty.Register("IsOpen", typeof(bool), typeof(PlaylistViewer), 
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.None, IsOpenPropertyChanged));

        private static void IsOpenPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            // If the drawer is opened, capture the mouse to be able to receive mouse-related events
            // such as PreviewMouseUpOutsideCapturedElementEvent that is handled inside class constructor
            if (d is PlaylistViewer instance)
            {
                if ((bool)e.NewValue == true)
                {
                    Mouse.Capture(instance, CaptureMode.SubTree);
                } else
                {
                    instance.ReleaseMouseCapture();
                }
            }
        }

        public bool IsOpen
        {
            get => (bool)GetValue(IsOpenProp); 
            set
            {
                SetValue(IsOpenProp, value);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsOpen)));
            }
        }

        public ObservableCollection<Item> Playlist { get; set; } = new ObservableCollection<Item>()
        {
            new Item("This is item 1 (a/b/c/d/e/f).g gggg", true),
            new Item("Item 2", false),
            new Item("Path/to/pppp", false),
        };

        public PlaylistViewer()
        {
            InitializeComponent();
            Mouse.AddPreviewMouseUpOutsideCapturedElementHandler(this, new MouseButtonEventHandler(Collapse));

            var recaptureMouseEventHandler = new MouseEventHandler(Recapture);
            (FindName("ListBox") as ListBox)?.AddHandler(Mouse.LostMouseCaptureEvent, recaptureMouseEventHandler);
        }

        private void Collapse(object sender, MouseButtonEventArgs e)
        {
            IsOpen = false;
        }

        // Clicking child elements inside this will make any mouse capture to lose effects due to the bubbling nature,
        // hence the workaround here
        private void Recapture(object sender, MouseEventArgs e)
        {
            if (Mouse.Captured == null)
            {
                Mouse.Capture(this, CaptureMode.SubTree);
            }
        }

        private void ListViewCrossIcon_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var selected = (Item)((PackIcon)sender).Tag; // used the Tag property as a way to store data in xaml
            Playlist.Remove(selected);
        }

        private void ListViewItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var selected = ((TextBlock)sender).Text;
            for (var i = 0; i < Playlist.Count; i++)
            {
                var p = Playlist[i];
                if (p.Name == selected && !p.IsSelected)
                {
                    Playlist[i] = new Item(selected, true); // highlight selected item
                }
                if (p.Name != selected && p.IsSelected)
                {
                    Playlist[i] = new Item(p.Name, false); // unselect current highlighted item
                }
            } 
        }
    }
}
