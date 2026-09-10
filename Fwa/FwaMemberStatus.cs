namespace Dwight;

public record FwaClanAssociation(string ClanName, string ClanTag);

public record FwaMemberStatus(bool IsBanned, FwaClanAssociation? BlacklistedClan);
