﻿using System;

namespace TranscribeService.Models
{
    public class SubtitleEntry
    {
        public int Index { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string Text { get; set; }
    }
}
