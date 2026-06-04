namespace Dwight;

public record Player(string Name, WarPreference WarPreference, int Donations, int DonationsReceived, int ClanCapitalContributions, Label[]? Labels, Achievement[]? Achievements);