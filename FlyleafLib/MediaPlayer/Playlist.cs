using System;
using System.Collections.Generic;

namespace FlyleafLib.MediaPlayer
{
    public class Playlist
    {
        private readonly Player player;

        private string current;
        private List<string> playlist = new();
        private readonly Stack<string> previous = new(); // played list

        private readonly Random random = new();

        public Playlist(Player player)
        {
            this.player = player;
            this.player.PlaybackCompleted += PlaybackCompleted;
        }

        public void Play(List<string> files)
        {
            if (files is not null && files.Count > 0)
            {
                playlist = files;
                player.Log.Info($"[Playlist] Total items in queue: {playlist.Count}");
                current = Properties.Settings.Default.Shuffled ? RandomPop() : Dequeue();
                player.OpenAsync(current);
            }
        }

        private void PlaybackCompleted(object sender, PlaybackCompletedArgs e)
        {
            if (playlist.Count > 0)
            {
                PlayNext();
            }
        }

        public void PlayNext()
        {
            if (player.IsPlaying)
            {
                player.Stop();
            }
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
