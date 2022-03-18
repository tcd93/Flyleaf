using System;
using System.Collections.Generic;

namespace FlyleafLib.MediaPlayer
{
    public class Playlist
    {
        private readonly Player player;
        private static Queue<string> playlist;

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
                playlist = new Queue<string>(files);
                player.Log.Info($"[Playlist] Total items in queue: {playlist.Count}");
                player.OpenAsync(playlist.Dequeue());
            }
        }

        private void PlaybackCompleted(object sender, PlaybackCompletedArgs e)
        {
            if (playlist.Count > 0)
            {
                player.Log.Info($"[Playlist] Play next item in list: {playlist.Peek()}");
                player.OpenAsync(playlist.Dequeue());
            }
        }
    }
}
