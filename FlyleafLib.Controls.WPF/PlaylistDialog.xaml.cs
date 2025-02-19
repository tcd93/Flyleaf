﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using FlyleafLib.MediaPlayer;
using MaterialDesignThemes.Wpf;

namespace FlyleafLib.Controls.WPF
{
    /// <summary>
    /// Interaction logic for PlaylistDialog.xaml
    /// </summary>
    public partial class PlaylistDialog : UserControl
    {
        public Player Player { get; }

        public PlaylistDialog(Flyleaf flyleaf)
        {
            InitializeComponent();
            Resources.SetTheme(flyleaf.Resources.GetTheme());
            Player = flyleaf.Player;
        }

        private void FilterTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var textBox = (TextBox)sender;
            if (e.Key == System.Windows.Input.Key.Tab)
            {
                if (textBox.Text != String.Empty)
                {
                    Player.MediaPlaylist.AddFilter(textBox.Text);
                    textBox.Clear();
                }
                e.Handled = true;
            } else if (e.Key == System.Windows.Input.Key.Back && textBox.Text == String.Empty)
            {
                Player.MediaPlaylist.RemoveLastFilter();
                e.Handled = true;
            }
        }

        private void Chip_DeleteClick(object sender, RoutedEventArgs e)
        {
            var chip = (Chip)sender;
            var deleted = ((TextBlock)chip.Content).Text;
            Player.MediaPlaylist.Filters.Remove(deleted);
        }
    }

    public partial class LastChildFillWrapPanel : WrapPanel
    {
        protected override Size ArrangeOverride(Size finalSize)
        {
            var wrapBoxSize = base.ArrangeOverride(finalSize);

            if (Children.Count > 0)
            {
                if (Children[Children.Count - 1] is FrameworkElement lastElem)
                {
                    var relativePosToParent = GetVisualChild(Children.Count - 1).TransformToAncestor(this).Transform(new Point(0, 0));
                    lastElem.Arrange(new Rect(relativePosToParent.X, relativePosToParent.Y, wrapBoxSize.Width - relativePosToParent.X, lastElem.ActualHeight));
                }
            }
            return wrapBoxSize;
        }
    }
}
