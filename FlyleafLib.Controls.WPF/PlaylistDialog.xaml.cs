using System.ComponentModel;
using System.Windows.Controls;

using MaterialDesignThemes.Wpf;

namespace FlyleafLib.Controls.WPF
{
    /// <summary>
    /// Interaction logic for PlaylistDialog.xaml
    /// </summary>
    public partial class PlaylistDialog : UserControl
    {
        public PlaylistDialog(Flyleaf flyleaf)
        {
            InitializeComponent();
            DataContext = flyleaf.DataContext;
            Resources.SetTheme(flyleaf.Resources.GetTheme());
        }
    }
}
