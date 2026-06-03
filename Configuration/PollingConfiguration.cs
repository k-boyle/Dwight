using System;

namespace Dwight;

public class PollingConfiguration
{
    // todo unify
    public bool RoleCheckingEnabled { get; set; }
    public TimeSpan RoleCheckingPollingDuration { get; set; }

    public bool WarReminderEnabled { get; set; }
    public TimeSpan WarReminderPollingDuration { get; set; }

    public bool ActivityTrackingEnabled { get; set; }
    public TimeSpan ActivityTrackingPollingDuration { get; set; }
    public TimeSpan ActivityRetentionPeriod { get; set; }
}
