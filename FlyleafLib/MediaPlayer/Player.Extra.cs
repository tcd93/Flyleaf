﻿using System;
using System.Drawing.Imaging;
using System.IO;
using System.Collections.Generic;

using FlyleafLib.MediaFramework.MediaDecoder;
using FlyleafLib.MediaFramework.MediaDemuxer;

using static FlyleafLib.Utils;
using static FlyleafLib.Logger;

namespace FlyleafLib.MediaPlayer
{
    unsafe partial class Player
    {
        public bool IsOpenFileDialogOpen    { get; private set; }

        public void SeekBackward()
        {
            if (!CanPlay) return;

            if (Config.Player.SeekAccurate)
                SeekAccurate(Math.Max((int) ((CurTime - Config.Player.SeekOffset) / 10000), 0));
            else
                Seek(Math.Max((int) ((CurTime - Config.Player.SeekOffset) / 10000), 0), false);
            
        }
        public void SeekBackward2()
        {
            if (!CanPlay) return;

            if (Config.Player.SeekAccurate)
                SeekAccurate(Math.Max((int) ((CurTime - Config.Player.SeekOffset2) / 10000), 0));
            else
                Seek(Math.Max((int) ((CurTime - Config.Player.SeekOffset2) / 10000), 0), false);
            
        }
        public void SeekForward()
        {
            if (!CanPlay) return;

            long seekTs = CurTime + Config.Player.SeekOffset;
            seekTs = seekTs > Duration ? Duration : seekTs;
            if (seekTs <= Duration || isLive)
            {
                if (Config.Player.SeekAccurate)
                    SeekAccurate((int)(seekTs / 10000));
                else
                    Seek((int)(seekTs / 10000), true);
            }
        }
        public void SeekForward2()
        {
            if (!CanPlay) return;

            long seekTs = CurTime + Config.Player.SeekOffset2;
            seekTs = seekTs > Duration ? Duration : seekTs;
            if (seekTs <= Duration || isLive)
            {
                if (Config.Player.SeekAccurate)
                    SeekAccurate((int)(seekTs / 10000));
                else
                    Seek((int)(seekTs / 10000), true);
            }
        }

        public void SeekToChapter(Demuxer.Chapter chapter)
        {
            /* TODO
             * Accurate pts required (backward/forward check)
             * Get current chapter implementation + next/prev
             */
            Seek((int) (chapter.StartTime / 10000.0), true);
        }

        public void CopyToClipboard()
        {
            if (decoder.Playlist.Url == null) 
                System.Windows.Clipboard.SetText("");
            else
                System.Windows.Clipboard.SetText(decoder.Playlist.Url);
        }
        public void CopyItemToClipboard()
        {
            if (decoder.Playlist.Selected == null || decoder.Playlist.Selected.DirectUrl == null)
                System.Windows.Clipboard.SetText("");
            else
                System.Windows.Clipboard.SetText(decoder.Playlist.Selected.DirectUrl);
        }
        public void OpenFromClipboard()
        {
            OpenAsync(System.Windows.Clipboard.GetText());
        }
        public void OpenFromFileDialog()
        {
            IsOpenFileDialogOpen = true;
            bool allowIdleMode = false;

            if (Config.Player.ActivityMode)
            {
                allowIdleMode = true;
                Config.Player.ActivityMode = false;
            }

            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
            var res = openFileDialog.ShowDialog();

            if(res == System.Windows.Forms.DialogResult.OK)
                OpenAsync(openFileDialog.FileName);

            if (allowIdleMode)
                Config.Player.ActivityMode = true;

            IsOpenFileDialogOpen = false;

            panClickX = -1; panClickY = -1;
            mouseDownPoint = new System.Drawing.Point(-1, -1);
        }

        public void OpenFromFolderDialog()
        {
            IsOpenFileDialogOpen = true;
            bool allowIdleMode = false;

            if (Config.Player.ActivityMode)
            {
                allowIdleMode = true;
                Config.Player.ActivityMode = false;
            }

            using (System.Windows.Forms.FolderBrowserDialog dialog = new())
            {
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    MediaPlaylist.Path = dialog.SelectedPath; // notify view
                }
            }

            if (allowIdleMode)
            {
                Config.Player.ActivityMode = true;
            }

            IsOpenFileDialogOpen = false;
        }

        public void ShowFrame(int frameIndex)
        {
            if (!Video.IsOpened || !CanPlay || VideoDemuxer.IsHLSLive) return;

            lock (lockActions)
            {
                Pause();
                sFrame = null;
                Subtitles.subsText = "";
                if (Subtitles._SubsText != "")
                    UI(() => Subtitles.SubsText = Subtitles.SubsText);
                decoder.Flush();
                decoder.RequiresResync = true;

                vFrame = VideoDecoder.GetFrame(frameIndex);
                if (vFrame == null) return;

                if (CanDebug) Log.Debug($"SFI: {VideoDecoder.GetFrameNumber(vFrame.timestamp)}");

                curTime = vFrame.timestamp;
                renderer.Present(vFrame);
                reversePlaybackResync = true;                
                vFrame = null;

                UI(() => UpdateCurTime());
            }
        }
        public void ShowFrameNext()
        {
            if (!Video.IsOpened || !CanPlay || VideoDemuxer.IsHLSLive) return;

            lock (lockActions)
            {
                Pause();
                ReversePlayback = false;
                if (Subtitles._SubsText != "")
                {
                    sFrame = null;
                    Subtitles.subsText = "";
                    Subtitles.SubsText = Subtitles.SubsText;
                }

                if (VideoDecoder.Frames.Count == 0)
                    vFrame = VideoDecoder.GetFrameNext();
                else
                    VideoDecoder.Frames.TryDequeue(out vFrame);

                if (vFrame == null) return;

                if (CanDebug) Log.Debug($"SFN: {VideoDecoder.GetFrameNumber(vFrame.timestamp)}");

                curTime = curTime = vFrame.timestamp;
                renderer.Present(vFrame);
                reversePlaybackResync = true;
                vFrame = null;

                UI(() => UpdateCurTime());
            }
        }
        public void ShowFramePrev()
        {
            if (!Video.IsOpened || !CanPlay || VideoDemuxer.IsHLSLive) return;

            lock (lockActions)
            {
                Pause();

                if (!ReversePlayback)
                {
                    Set(ref _ReversePlayback, true, false);
                    Speed = 1;
                    if (Subtitles._SubsText != "")
                    {
                        sFrame = null;
                        Subtitles.subsText = "";
                        Subtitles.SubsText = Subtitles.SubsText;
                    }
                    decoder.StopThreads();
                    decoder.Flush();
                }

                if (VideoDecoder.Frames.Count == 0)
                {
                    // Temp fix for previous timestamps until we seperate GetFrame for Extractor and the Player
                    reversePlaybackResync = true;
                    int askedFrame = VideoDecoder.GetFrameNumber(CurTime) - 1;
                    vFrame = VideoDecoder.GetFrame(askedFrame);
                    if (vFrame == null) return;

                    int recvFrame = VideoDecoder.GetFrameNumber(vFrame.timestamp);
                    if (askedFrame != recvFrame)
                    {
                        VideoDecoder.DisposeFrame(vFrame);
                        vFrame = null;
                        if (askedFrame > recvFrame)
                            vFrame = VideoDecoder.GetFrame(VideoDecoder.GetFrameNumber(CurTime));
                        else
                            vFrame = VideoDecoder.GetFrame(VideoDecoder.GetFrameNumber(CurTime) - 2);
                    }
                }
                else
                    VideoDecoder.Frames.TryDequeue(out vFrame);

                if (vFrame == null) return;

                if (CanDebug) Log.Debug($"SFB: {VideoDecoder.GetFrameNumber(vFrame.timestamp)}");

                curTime = vFrame.timestamp;
                renderer.Present(vFrame);
                vFrame = null;
                UI(() => UpdateCurTime()); // For some strange reason this will not be updated on KeyDown (only on KeyUp) which doesn't happen on ShowFrameNext (GPU overload? / Thread.Sleep underlying in UI thread?)
            }
        }

        public void SpeedUp()
        {
            if (Speed + 0.25 > 1 && ReversePlayback)
                return;

            Speed = Speed + 0.25 > 16 ? 16 : Speed + 0.25;
        }
        public void SpeedDown()
        {
            Speed = Speed - 0.25 < 0.25 ? 0.25 : Speed - 0.25;
        }
        
        public void FullScreen()
        {
            if (IsFullScreen) return;

            if (    (VideoView  != null && VideoView.FullScreen()) 
                ||  (Control    != null && Control.FullScreen()))
                IsFullScreen = true;
        }
        public void NormalScreen()
        {
            if (!IsFullScreen) return;

            if (    (VideoView  != null && VideoView.NormalScreen()) 
                ||  (Control    != null && Control.NormalScreen()))
                IsFullScreen = false;
        }
        public void ToggleFullScreen()
        {
            if (IsFullScreen)
                NormalScreen();
            else
                FullScreen();
        }

        /// <summary>
        /// Starts recording (uses Config.Player.FolderRecordings and default filename title_curTime)
        /// </summary>
        public void StartRecording()
        {
            if (!CanPlay)
                return;
            try
            {
                if (!Directory.Exists(Config.Player.FolderRecordings))
                    Directory.CreateDirectory(Config.Player.FolderRecordings);

                string filename = Utils.GetValidFileName(string.IsNullOrEmpty(Playlist.Selected.Title) ? "Record" : Playlist.Selected.Title) + $"_{(new TimeSpan(CurTime)).ToString("hhmmss")}." + decoder.Extension;
                filename = Utils.FindNextAvailableFile(Path.Combine(Config.Player.FolderRecordings, filename));
                StartRecording(ref filename, false);
            } catch { }
        }

        /// <summary>
        /// Starts recording
        /// </summary>
        /// <param name="filename">Path of the new recording file</param>
        /// <param name="useRecommendedExtension">You can force the output container's format or use the recommended one to avoid incompatibility</param>
        public void StartRecording(ref string filename, bool useRecommendedExtension = true)
        {
            if (!CanPlay)
                return;

            decoder.StartRecording(ref filename, useRecommendedExtension);
            IsRecording = decoder.IsRecording;
        }

        /// <summary>
        /// Stops recording
        /// </summary>
        public void StopRecording()
        {
            decoder.StopRecording();
            IsRecording = decoder.IsRecording;
        }
        public void ToggleRecording()
        {
            if (!CanPlay) return;

            if (IsRecording)
                StopRecording();
            else
                StartRecording();
        }

        /// <summary>
        /// <para>Saves the current video frame (encoding based on file extention .bmp, .png, .jpg)</para>
        /// <para>If filename not specified will use Config.Player.FolderSnapshots and with default filename title_frameNumber.ext (ext from Config.Player.SnapshotFormat)</para>
        /// <para>If width/height not specified will use the original size. If one of them will be set, the other one will be set based on original ratio</para>
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="height">Specify the height (-1: will keep the ratio based on width)</param>
        /// <param name="width">Specify the width (-1: will keep the ratio based on height)</param>
        /// <exception cref="Exception"></exception>
        public void TakeSnapshot(string filename = null, int height = -1, int width = -1)
        {
            if (!CanPlay) return;

            if (filename == null)
            {
                try
                {
                    if (!Directory.Exists(Config.Player.FolderSnapshots))
                        Directory.CreateDirectory(Config.Player.FolderSnapshots);

                    filename = GetValidFileName(string.IsNullOrEmpty(Playlist.Selected.Title) ? "Snapshot" : Playlist.Selected.Title) + $"_{VideoDecoder.GetFrameNumber(CurTime)}.{Config.Player.SnapshotFormat}";
                    filename = FindNextAvailableFile(Path.Combine(Config.Player.FolderSnapshots, filename));
                } catch { return; }
            }

            string ext = GetUrlExtention(filename);

            ImageFormat imageFormat;

            switch (ext)
            {
                case "bmp":
                    imageFormat = ImageFormat.Bmp;
                    break;

                case "png":
                    imageFormat = ImageFormat.Png;
                    break;

                case "jpg":
                case "jpeg":
                    imageFormat = ImageFormat.Jpeg;
                    break;

                default:
                    throw new Exception($"Invalid snapshot extention '{ext}' (valid .bmp, .png, .jpeg, .jpg");
            }

            renderer?.TakeSnapshot(filename, imageFormat, height, width);
        }

        public void ZoomIn()
        {
            Zoom += Config.Player.ZoomOffset;
        }
        public void ZoomOut()
        {
            Zoom -= Config.Player.ZoomOffset;

            if (renderer.GetViewport.Width < 0 || renderer.GetViewport.Height < 0)
                Zoom += Config.Player.ZoomOffset;
        }

        public void ResetAll()
        {
            Speed = 1;
            PanXOffset = 0;
            PanYOffset = 0;
            Zoom = 0;
            ReversePlayback = false;
        }
    }
}
