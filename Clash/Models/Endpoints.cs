namespace Dwight;

public static class Endpoints
{
    public static readonly Endpoint<PagedClanMembers> GetClanMembers = new("/v1/clans/{0}/members");
    public static readonly Endpoint<CurrentWar> GetCurrentWar = new("/v1/clans/{0}/currentwar");
    public static readonly Endpoint<LeagueGroup> GetLeagueGroup = new("/v1/clans/{0}/currentwar/leaguegroup");
    public static readonly Endpoint<CurrentWar> GetLeagueWar = new("/v1/clanwarleagues/wars/{0}");
    public static readonly Endpoint<Player> GetPlayer = new("/v1/players/{0}");
}
