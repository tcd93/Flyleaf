using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.ComponentModel;

namespace FlyleafLib.MediaPlayer
{
    public class MediaPlaylist : INotifyPropertyChanged
    {
        private readonly Player player;

        private string _path = String.Empty;
        private List<string> playlist = new(); // remaining
        private readonly List<string> previous = new(); // played list

        private readonly Random random = new();

        public event PropertyChangedEventHandler PropertyChanged;

        public string Path
        {
            get => _path;
            set {
                _path = value ?? String.Empty;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Path)));
            }
        }

        private bool _openSideView;
        public bool OpenSideView { 
            get => _openSideView; 
            set {
                _openSideView = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OpenSideView)));
            }
        }

        private string _current = "";
        /// <summary>
        /// Bound by view model (PlaylistViewer.xaml), UI modifications are reflected here.
        /// Setting this value plays the media immediately
        /// </summary>
        public string Current
        {
            get => _current;
            set
            {
                if (value == _current) return;
                _current = value ?? "";
                if (!string.IsNullOrEmpty(_current)) player.OpenAsync(_current);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Current)));
            }
        }

        private ObservableCollection<string> _items = new();
        /// <summary>
        /// Bound by view model (PlaylistViewer.xaml), UI modifications are reflected here
        /// </summary>
        public ObservableCollection<string> Items { 
            get => _items; 
            set
            {
                _items = value;
                Items.CollectionChanged -= Items_CollectionChanged;
                Items.CollectionChanged += Items_CollectionChanged;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Items)));
            }
        }

        public ObservableCollection<string> Filters { get; set; } = new();

        public MediaPlaylist(Player player)
        {
            this.player = player;
            this.player.PlaybackStopped += PlaybackStopped;
        }

        public void AddFilter(string filter)
        {
            if (filter.Trim() != string.Empty)
            {
                Filters.Add(filter.Trim());
            }
        }

        public void RemoveLastFilter()
        {
            if (Filters.Count > 0)
            {
                Filters.RemoveAt(Filters.Count - 1);
            }
        }

        public void ToggleSideView()
        {
            if (playlist is not null && playlist.Count > 0)
            {
                OpenSideView = !OpenSideView;
            }
        }

        public void Play()
        {
            playlist = Utils.FindMovFilesInPath(_path)
                .Where(media => Filters.All(filter => media.ToLower().Contains(filter.ToLower()))).ToList();

            Items = new ObservableCollection<string>(playlist);

            if (playlist is not null && playlist.Count > 0)
            {
                player.Log.Info($"[Playlist] Total items in queue: {playlist.Count}");
                Current = Properties.Settings.Default.Shuffled ? RandomPop() : Dequeue();
            } else
            {
                player.Log.Warn("No playlist in current playlist");
                // TODO: update source to show error
            }
        }

        private void Items_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove && e.OldItems != null)
            {
                foreach (string changedItem in e.OldItems)
                {
                    if (Current == changedItem)
                    {
                        PlayNext();
                    }
                    Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(changedItem,
                            Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                            Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin,
                            Microsoft.VisualBasic.FileIO.UICancelOption.DoNothing);

                    playlist.Remove(changedItem);
                    previous.Remove(changedItem);
                }
            }
        }

        private void PlaybackStopped(object sender, PlaybackStoppedArgs e)
        {
            if (playlist.Count > 0 && player.Status == Status.Ended)
            {
                PlayNext();
            }
        }

        public void PlayNext()
        {
            lock (playlist) {
                if (playlist.Count == 0 || Current is null)
                {
                    return;
                }
                previous.Add(Current); // archive current item
                Current = Properties.Settings.Default.Shuffled ? RandomPop() : Dequeue();
            }
        }

        public void PlayPrevious()
        {
            if (previous.Count == 0)
            {
                return;
            }
            lock (previous)
            {
                if (previous.Count == 0 || Current is null)
                {
                    return;
                }
                playlist.Insert(0, Current); // put current item back into front queue
                Current = previous[previous.Count - 1];
                previous.RemoveAt(previous.Count - 1);
            }
        }

        private string Dequeue()
        {
            string first = playlist[0];
            playlist.RemoveAt(0);
            return first;
        }

        private string RandomPop()
        {
            int rnd = random.Next(playlist.Count);
            string s = playlist[rnd];
            player.Log.Info($"[Playlist] Random play next item in list: {s}");
            playlist.RemoveAt(rnd);
            return s;
        }
    }
}
