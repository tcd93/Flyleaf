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
        private string current;
        private List<string> playlist = new();
        private readonly Stack<string> previous = new(); // played list

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

        public void Play()
        {
            playlist = Utils.FindMovFilesInPath(_path)
                .Where(media => Filters.All(filter => media.ToLower().Contains(filter.ToLower()))).ToList();

            if (playlist is not null && playlist.Count > 0)
            {
                player.Log.Info($"[Playlist] Total items in queue: {playlist.Count}");
                current = Properties.Settings.Default.Shuffled ? RandomPop() : Dequeue();
                player.OpenAsync(current);
            } else
            {
                player.Log.Warn("No playlist in current playlist");
                // TODO: update source to show error
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
                if (playlist.Count == 0 || current is null)
                {
                    return;
                }
                previous.Push(current); // archive current item
                current = Properties.Settings.Default.Shuffled ? RandomPop() : Dequeue();
                player.OpenAsync(current);
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
                if (previous.Count == 0 || current is null)
                {
                    return;
                }
                playlist.Insert(0, current); // put current item back into front queue
                current = previous.Pop();
                player.OpenAsync(current);
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
