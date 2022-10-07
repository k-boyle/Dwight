namespace Dwight;

public class CurrentWarReminder
{
    public ulong GuildId { get; set; }

    public string EnemyClan { get; set; }
    public bool DeclaredPosted { get; set; }
    public bool StartedPosted { get; set; }
    public int CwlRemindersPosted { get; set; }
    public int CwlReminderLastPosted { get; set; }
    public bool ReminderPosted { get; set; }

    public CurrentWarReminder(ulong guildId, string enemyClan)
    {
        GuildId = guildId;
        EnemyClan = enemyClan;
    }
}