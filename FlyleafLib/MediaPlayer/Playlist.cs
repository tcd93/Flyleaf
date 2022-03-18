using System;
using System.Collections.Generic;

namespace FlyleafLib.MediaPlayer
{
    public class Playlist
    {
        private readonly Player player;
        private static List<string> playlist;

        private readonly Random random = new();

        public bool Shuffled { get; set; }

        public Playlist(Player player)
        {
            this.player = player;
            this.player.PlaybackCompleted += PlaybackCompleted;
        }

        public void Play(List<string> files)
        {
            if (player.IsPlaying)
            {
                player.Stop();
            }

            if (files is not null && files.Count > 0)
            {
                playlist = files;
                player.Log.Info($"[Playlist] Total items in queue: {playlist.Count}");
                PlayNext();
            }
        }

        private void PlaybackCompleted(object sender, PlaybackCompletedArgs e)
        {
            if (playlist.Count > 0)
            {
                PlayNext();
            }
        }

        private void PlayNext()
        {
            player.OpenAsync(Shuffled ? RandomPop() : Dequeue());
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
