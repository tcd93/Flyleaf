﻿using System;

namespace FlyleafLib.MediaFramework.MediaStream
{
    public class ExternalAudioStream : ExternalStream
    {
        public int      SampleRate      { get; set; }
        public string   ChannelLayout   { get; set; }
        public Language Language        { get; set; }

        public bool     HasVideo        { get; set; }
    }
}
