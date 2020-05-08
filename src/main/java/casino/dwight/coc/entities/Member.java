package casino.dwight.coc.entities;

import com.fasterxml.jackson.annotation.JsonProperty;

public class Member {
    private String tag;
    private String name;
    private long townhallLevel;
    private long mapPosition;
    private long opponentAttacks;
    private Role role;
    private long expLevel;
    private League league;
    private long trophies;
    private long versusTrophies;
    private long clanRank;
    private long previousClanRank;
    private long donations;
    private long donationsReceived;

    @JsonProperty("tag")
    public String getTag() { return tag; }
    @JsonProperty("tag")
    public void setTag(String value) { this.tag = value; }

    @JsonProperty("name")
    public String getName() { return name; }
    @JsonProperty("name")
    public void setName(String value) { this.name = value; }

    @JsonProperty("townhallLevel")
    public long getTownhallLevel() { return townhallLevel; }
    @JsonProperty("townhallLevel")
    public void setTownhallLevel(long value) { this.townhallLevel = value; }

    @JsonProperty("mapPosition")
    public long getMapPosition() { return mapPosition; }
    @JsonProperty("mapPosition")
    public void setMapPosition(long value) { this.mapPosition = value; }

    @JsonProperty("opponentAttacks")
    public long getOpponentAttacks() { return opponentAttacks; }
    @JsonProperty("opponentAttacks")
    public void setOpponentAttacks(long value) { this.opponentAttacks = value; }

    @JsonProperty("role")
    public Role getRole() { return role; }
    @JsonProperty("role")
    public void setRole(Role value) { this.role = value; }

    @JsonProperty("expLevel")
    public long getExpLevel() { return expLevel; }
    @JsonProperty("expLevel")
    public void setExpLevel(long value) { this.expLevel = value; }

    @JsonProperty("league")
    public League getLeague() { return league; }
    @JsonProperty("league")
    public void setLeague(League value) { this.league = value; }

    @JsonProperty("trophies")
    public long getTrophies() { return trophies; }
    @JsonProperty("trophies")
    public void setTrophies(long value) { this.trophies = value; }

    @JsonProperty("versusTrophies")
    public long getVersusTrophies() { return versusTrophies; }
    @JsonProperty("versusTrophies")
    public void setVersusTrophies(long value) { this.versusTrophies = value; }

    @JsonProperty("clanRank")
    public long getClanRank() { return clanRank; }
    @JsonProperty("clanRank")
    public void setClanRank(long value) { this.clanRank = value; }

    @JsonProperty("previousClanRank")
    public long getPreviousClanRank() { return previousClanRank; }
    @JsonProperty("previousClanRank")
    public void setPreviousClanRank(long value) { this.previousClanRank = value; }

    @JsonProperty("donations")
    public long getDonations() { return donations; }
    @JsonProperty("donations")
    public void setDonations(long value) { this.donations = value; }

    @JsonProperty("donationsReceived")
    public long getDonationsReceived() { return donationsReceived; }
    @JsonProperty("donationsReceived")
    public void setDonationsReceived(long value) { this.donationsReceived = value; }

}
