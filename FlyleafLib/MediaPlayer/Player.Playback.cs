﻿using System;
using System.Threading;
using System.Threading.Tasks;

using FlyleafLib.MediaFramework.MediaDecoder;

using static FlyleafLib.Utils;
using static FlyleafLib.Utils.NativeMethods;
using static FlyleafLib.Logger;

namespace FlyleafLib.MediaPlayer
{
    partial class Player
    {
        bool stoppedWithError;

        /// <summary>
        /// Fires on playback stopped by an error or completed / ended successfully <see cref="Status"/>
        /// Warning: Uses Invoke and it comes from playback thread so you can't pause/stop etc. You need to use another thread if you have to.
        /// </summary>
        public event EventHandler<PlaybackStoppedArgs> PlaybackStopped;
        protected virtual void OnPlaybackStopped(string error = null)
        {
            if (error != null && LastError == null)
            {
                lastError = error;
                UI(() => LastError = LastError);
            }

            PlaybackStopped?.Invoke(this, new PlaybackStoppedArgs(error));
        }

        /// <summary>
        /// Plays AVS streams
        /// </summary>
        public void Play()
        {
            lock (lockActions)
            {
                if (!CanPlay || Status == Status.Playing || Status == Status.Ended)
                    return;

                status = Status.Playing;
                UI(() => Status = Status);
            }

            while (taskPlayRuns || taskSeekRuns) Thread.Sleep(5);
            taskPlayRuns = true;

            // Long-Run Task
            Task.Factory.StartNew(() =>
            {
                try
                {
                    TimeBeginPeriod(1);
                    SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_SYSTEM_REQUIRED | EXECUTION_STATE.ES_DISPLAY_REQUIRED);

                    onBufferingStarted   = 0;
                    onBufferingCompleted = 0;

                    if (LastError != null)
                    {
                        lastError = null;
                        UI(() => LastError = LastError);
                    }

                    if (Config.Player.Usage == Usage.LowLatencyVideo)
                        ScreamerLowLatency();
                    else if (Config.Player.Usage == Usage.Audio || !Video.IsOpened)
                        ScreamerAudioOnly();
                    else
                    {
                        if (ReversePlayback)
                            ScreamerReverse();
                        else
                            Screamer();
                    }

                } catch (Exception e)
                {
                    Log.Error($"Playback failed ({e.Message})");
                }
                finally
                {
                    VideoDecoder.DisposeFrame(vFrame);
                    vFrame = null;
                        
                    if (Status == Status.Stopped)
                        decoder?.Initialize();
                    else if (decoder != null) 
                    {
                        decoder.PauseOnQueueFull();
                        decoder.PauseDecoders();
                    }

                    Audio.ClearBuffer();
                    TimeEndPeriod(1);
                    SetThreadExecutionState(EXECUTION_STATE.ES_CONTINUOUS);
                    stoppedWithError = false;

                    if (IsPlaying)
                    {    
                        if (decoderHasEnded)
                            status = Status.Ended;
                        else
                        {
                            if (onBufferingStarted - 1 == onBufferingCompleted)
                            {
                                stoppedWithError = true;
                                OnBufferingCompleted("Buffering failed");
                            }
                            else
                            {
                                if (!ReversePlayback)
                                    stoppedWithError = isLive || Math.Abs(Duration - CurTime) > 3 * 1000 * 10000;
                                else
                                    stoppedWithError = CurTime > 3 * 1000 * 10000;
                            }

                            status = Status.Paused;
                        }
                    }
                        
                    OnPlaybackStopped(stoppedWithError ? "Playback stopped unexpectedly" : null);
                    if (CanDebug) Log.Debug($"[SCREAMER] Finished (Status: {Status}, Error: {(stoppedWithError ? "Playback stopped unexpectedly" : "")})");

                    UI(() =>
                    {
                        Status = Status;
                        UpdateCurTime();
                    });

                    taskPlayRuns = false;
                }
            }, TaskCreationOptions.LongRunning);
        }

        /// <summary>
        /// Play & skip until last 10% of the media duration
        /// </summary>
        public void PlayToNearEnd()
        {
            if (Duration < long.MaxValue)
            {
                SeekAccurate(Convert.ToInt32(Duration * 9 / 10 / 10000));
            }
        }

        /// <summary>
        /// Pauses AVS streams
        /// </summary>
        public void Pause()
        {
            lock (lockActions)
            {
                if (!CanPlay || Status == Status.Ended)
                    return;

                status = Status.Paused;
                UI(() => Status = Status);

                while (taskPlayRuns) Thread.Sleep(5);
            }
        }

        public void TogglePlayPause()
        {
            if (IsPlaying)
                Pause();
            else 
                Play();
        }

        public void ToggleReversePlayback()
        {
            ReversePlayback = !ReversePlayback;
        }

        /// <summary>
        /// Seeks backwards or forwards based on the specified ms to the nearest keyframe
        /// </summary>
        /// <param name="ms"></param>
        /// <param name="forward"></param>
        public void Seek(int ms, bool forward = false)
        {
            Seek(ms, forward, false);
        }

        /// <summary>
        /// Seeks at the exact timestamp (with half frame distance accuracy)
        /// </summary>
        /// <param name="ms"></param>
        public void SeekAccurate(int ms)
        {
            Seek(ms, false, !IsLive);
        }

        public void ToggleSeekAccurate()
        {
            Config.Player.SeekAccurate = !Config.Player.SeekAccurate;
        }

        private void Seek(int ms, bool forward, bool accurate)
        {
            if (!CanPlay) return;

            curTime = ms * (long)10000;
            Raise(nameof(CurTime));
            seeks.Push(new SeekData(ms, forward, accurate));

            decoder.OpenedPlugin?.OnBuffering();

            if (Status == Status.Playing) return;

            lock (lockActions) { if (taskSeekRuns) return; taskSeekRuns = true; }

            Task.Run(() =>
            {
                int ret;
                bool wasEnded = false;

                try
                {
                    TimeBeginPeriod(1);
                    
                    while (seeks.TryPop(out SeekData seekData) && CanPlay && !IsPlaying)
                    {
                        seeks.Clear();

                        if (Status == Status.Ended)
                        {
                            wasEnded = true;
                            status = Status.Paused;
                            UI(() => Status = Status);
                        }

                        if (!Video.IsOpened)
                        {
                            if (AudioDecoder.OnVideoDemuxer)
                            {
                                ret = decoder.Seek(seekData.ms, seekData.forward);
                                if (CanWarn && ret < 0)
                                    Log.Warn("Seek failed 2");

                                VideoDemuxer.Start();
                            }
                            else
                            {
                                ret = decoder.SeekAudio(seekData.ms, seekData.forward);
                                if (CanWarn && ret < 0)
                                    Log.Warn("Seek failed 3");

                                AudioDemuxer.Start();
                            }

                            decoder.PauseOnQueueFull();
                        }
                        else
                        {
                            decoder.PauseDecoders();
                            ret = decoder.Seek(seekData.ms, seekData.forward, !seekData.accurate);
                            if (ret < 0)
                            {
                                if (CanWarn) Log.Warn("Seek failed");
                            }
                            else if (!ReversePlayback && CanPlay)
                            {
                                decoder.GetVideoFrame(seekData.accurate ? seekData.ms * (long)10000 : -1);
                                ShowOneFrame();
                                VideoDemuxer.Start();
                                AudioDemuxer.Start();
                                SubtitlesDemuxer.Start();
                                decoder.PauseOnQueueFull();
                            }
                        }

                        Thread.Sleep(20);
                    }
                } catch (Exception e)
                {
                    Log.Error($"Seek failed ({e.Message})");
                } finally
                {
                    decoder.OpenedPlugin?.OnBufferingCompleted();
                    TimeEndPeriod(1);
                    lock (lockActions) taskSeekRuns = false;
                    if ((wasEnded && Config.Player.AutoPlay) || stoppedWithError)
                        Play();
                }
            });
        }

        /// <summary>
        /// Flushes the buffer (demuxers (packets) and decoders (frames))
        /// This is useful mainly for live streams to push the playback at very end (low latency)
        /// </summary>
        public void Flush()
        {
            decoder.Flush();
        }

        /// <summary>
        /// Stops and Closes AVS streams
        /// </summary>
        public void Stop()
        {
            lock (lockActions)
            {
                Initialize();
                VideoDecoder.DisposeVA();
            }
        }
    }

    public class PlaybackStoppedArgs : EventArgs
    {
        public string   Error       { get; }
        public bool     Success     { get; }
            
        public PlaybackStoppedArgs(string error)
        {
            Error   = error;
            Success = Error == null;
        }
    }

    class SeekData
    {
        public int  ms;
        public bool forward;
        public bool accurate;
        public SeekData(int ms, bool forward, bool accurate)
            { this.ms = ms; this.forward = forward && !accurate; this.accurate = accurate; }
    }

}
