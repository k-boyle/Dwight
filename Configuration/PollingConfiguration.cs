using System;

namespace Dwight;

public class PollingConfiguration
{
    public bool RoleCheckingEnabled { get; set; }
    public TimeSpan RoleCheckingPollingDuration { get; set; }

    public bool WarReminderEnabled { get; set; }
    public TimeSpan WarReminderPollingDuration { get; set; }
}
