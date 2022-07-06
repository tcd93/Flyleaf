using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using MaterialDesignThemes.Wpf;

namespace FlyleafLib.Controls.WPF
{
    /// <summary>
    /// Interaction logic for PlaylistViewer.xaml
    /// </summary>
    public partial class PlaylistViewer : UserControl, INotifyPropertyChanged
    {
        /// The item in playlist, and the current status (playing/non-playing)
        public struct Item
        {
            public Item(string name, bool isSelected)
            {
                Name = name;
                IsSelected = isSelected;
            }
            public string Name { get; }
            public string ShortName { get => Path.GetFileName(Name); }
            public bool IsSelected { get; }
        }

        #region Properties

        public static readonly DependencyProperty IsOpenProp =
            DependencyProperty.Register("IsOpen", typeof(bool), typeof(PlaylistViewer),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, IsOpenPropertyChanged));

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

        public bool IsOpen { get => (bool)GetValue(IsOpenProp); set => SetValue(IsOpenProp, value); }

        public static readonly DependencyProperty CurrentItemProp =
            DependencyProperty.Register("CurrentItem", typeof(string), typeof(PlaylistViewer),
                new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, CurrentItemPropertyChanged));

        private static void CurrentItemPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PlaylistViewer instance)
            {
                instance.RefreshList((string)e.NewValue);
            }
        }

        public string CurrentItem { get => (string)GetValue(CurrentItemProp); set => SetValue(CurrentItemProp, value); }

        public static readonly DependencyProperty ItemsSourceProp =
            DependencyProperty.Register("ItemsSource", typeof(ObservableCollection<string>), typeof(PlaylistViewer),
                new FrameworkPropertyMetadata(default(ICollection<string>), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, ItemsSourcePropertyChanged));

        private static void ItemsSourcePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PlaylistViewer instance)
            {
                var value = (ObservableCollection<string>)e.NewValue;
                instance.Items = new ObservableCollection<Item>(value.Select((s) => new Item(s, s == instance.CurrentItem)));
            }
        }

        public ICollection<string> ItemsSource { get => (ICollection<string>)GetValue(ItemsSourceProp); set => SetValue(ItemsSourceProp, value); }

        #endregion Properties

        public event PropertyChangedEventHandler PropertyChanged;
        private ObservableCollection<Item> _items = new ObservableCollection<Item>();
        private ListBox listBox;

        // The wrapped list linked with input Item Source, used for UI purposes
        public ObservableCollection<Item> Items { 
            get => _items; 
            private set
            {
                _items = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Items)));
            }
        }

        public PlaylistViewer()
        {
            InitializeComponent();
            Mouse.AddPreviewMouseUpOutsideCapturedElementHandler(this, new MouseButtonEventHandler(Collapse));

            var recaptureMouseEventHandler = new MouseEventHandler(Recapture);
            listBox = FindName("ListBox") as ListBox;
            listBox?.AddHandler(Mouse.LostMouseCaptureEvent, recaptureMouseEventHandler);
        }

        public void RefreshList(string current)
        {
            for (var i = 0; i < Items.Count; i++)
            {
                var p = Items[i];
                if (p.Name == current && !p.IsSelected)
                {
                    Items[i] = new Item(current, true); // highlight current item
                    listBox?.ScrollIntoView(Items[i]);
                }
                if (p.Name != current && p.IsSelected)
                {
                    Items[i] = new Item(p.Name, false); // unselect previous highlighted item
                }
            }
        }

        private void Collapse(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource == this)
            {
                IsOpen = false;
                var currentItem = Items.Where(i => i.Name == CurrentItem).FirstOrDefault();
                listBox?.ScrollIntoView(currentItem);
            }
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
            Items.Remove(selected);
            ItemsSource?.Remove(selected.Name);
        }

        private void ListViewItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            string selected = (string)((TextBlock)sender).Tag; // full path name
            RefreshList(selected);
            CurrentItem = selected;
        }

        private void TextBlock_ToolTipOpening(object sender, ToolTipEventArgs e)
        {
            var selected = (string)((TextBlock)sender).Tag; // full path name
            var fileInfo = new FileInfo(selected);
            ((TextBlock)sender).ToolTip = Math.Round(fileInfo.Length / 1024.0 / 1024.0, 2) + " MB";
        }
    }
}
