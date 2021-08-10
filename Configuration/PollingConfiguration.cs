using System;

namespace Dwight
{
    public class PollingConfiguration
    {
        public bool StartTimeEnabled { get; set; }
        public TimeSpan StartTimePollingDuration { get; set; }
    }
}