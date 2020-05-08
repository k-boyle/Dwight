package casino.dwight.coc.entities;

import com.fasterxml.jackson.annotation.JsonProperty;

public class Player {
    private String tag;
    private String name;
    private long townHallLevel;
    private long townHallWeaponLevel;
    private long expLevel;
    private long trophies;
    private long bestTrophies;
    private long warStars;
    private long attackWins;
    private long defenseWins;
    private long builderHallLevel;
    private long versusTrophies;
    private long bestVersusTrophies;
    private long versusBattleWins;
    private String role;
    private long donations;
    private long donationsReceived;
    private Clan clan;
    private League league;
    private Achievement[] achievements;
    private long versusBattleWinCount;
    private Label[] labels;
    private Hero[] troops;
    private Hero[] heroes;
    private Hero[] spells;

    @JsonProperty("tag")
    public String getTag() { return tag; }
    @JsonProperty("tag")
    public void setTag(String value) { this.tag = value; }

    @JsonProperty("name")
    public String getName() { return name; }
    @JsonProperty("name")
    public void setName(String value) { this.name = value; }

    @JsonProperty("townHallLevel")
    public long getTownHallLevel() { return townHallLevel; }
    @JsonProperty("townHallLevel")
    public void setTownHallLevel(long value) { this.townHallLevel = value; }

    @JsonProperty("townHallWeaponLevel")
    public long getTownHallWeaponLevel() { return townHallWeaponLevel; }
    @JsonProperty("townHallWeaponLevel")
    public void setTownHallWeaponLevel(long value) { this.townHallWeaponLevel = value; }

    @JsonProperty("expLevel")
    public long getExpLevel() { return expLevel; }
    @JsonProperty("expLevel")
    public void setExpLevel(long value) { this.expLevel = value; }

    @JsonProperty("trophies")
    public long getTrophies() { return trophies; }
    @JsonProperty("trophies")
    public void setTrophies(long value) { this.trophies = value; }

    @JsonProperty("bestTrophies")
    public long getBestTrophies() { return bestTrophies; }
    @JsonProperty("bestTrophies")
    public void setBestTrophies(long value) { this.bestTrophies = value; }

    @JsonProperty("warStars")
    public long getWarStars() { return warStars; }
    @JsonProperty("warStars")
    public void setWarStars(long value) { this.warStars = value; }

    @JsonProperty("attackWins")
    public long getAttackWins() { return attackWins; }
    @JsonProperty("attackWins")
    public void setAttackWins(long value) { this.attackWins = value; }

    @JsonProperty("defenseWins")
    public long getDefenseWins() { return defenseWins; }
    @JsonProperty("defenseWins")
    public void setDefenseWins(long value) { this.defenseWins = value; }

    @JsonProperty("builderHallLevel")
    public long getBuilderHallLevel() { return builderHallLevel; }
    @JsonProperty("builderHallLevel")
    public void setBuilderHallLevel(long value) { this.builderHallLevel = value; }

    @JsonProperty("versusTrophies")
    public long getVersusTrophies() { return versusTrophies; }
    @JsonProperty("versusTrophies")
    public void setVersusTrophies(long value) { this.versusTrophies = value; }

    @JsonProperty("bestVersusTrophies")
    public long getBestVersusTrophies() { return bestVersusTrophies; }
    @JsonProperty("bestVersusTrophies")
    public void setBestVersusTrophies(long value) { this.bestVersusTrophies = value; }

    @JsonProperty("versusBattleWins")
    public long getVersusBattleWins() { return versusBattleWins; }
    @JsonProperty("versusBattleWins")
    public void setVersusBattleWins(long value) { this.versusBattleWins = value; }

    @JsonProperty("role")
    public String getRole() { return role; }
    @JsonProperty("role")
    public void setRole(String value) { this.role = value; }

    @JsonProperty("donations")
    public long getDonations() { return donations; }
    @JsonProperty("donations")
    public void setDonations(long value) { this.donations = value; }

    @JsonProperty("donationsReceived")
    public long getDonationsReceived() { return donationsReceived; }
    @JsonProperty("donationsReceived")
    public void setDonationsReceived(long value) { this.donationsReceived = value; }

    @JsonProperty("clan")
    public Clan getClan() { return clan; }
    @JsonProperty("clan")
    public void setClan(Clan value) { this.clan = value; }

    @JsonProperty("league")
    public League getLeague() { return league; }
    @JsonProperty("league")
    public void setLeague(League value) { this.league = value; }

    @JsonProperty("achievements")
    public Achievement[] getAchievements() { return achievements; }
    @JsonProperty("achievements")
    public void setAchievements(Achievement[] value) { this.achievements = value; }

    @JsonProperty("versusBattleWinCount")
    public long getVersusBattleWinCount() { return versusBattleWinCount; }
    @JsonProperty("versusBattleWinCount")
    public void setVersusBattleWinCount(long value) { this.versusBattleWinCount = value; }

    @JsonProperty("labels")
    public Label[] getLabels() { return labels; }
    @JsonProperty("labels")
    public void setLabels(Label[] value) { this.labels = value; }

    @JsonProperty("troops")
    public Hero[] getTroops() { return troops; }
    @JsonProperty("troops")
    public void setTroops(Hero[] value) { this.troops = value; }

    @JsonProperty("heroes")
    public Hero[] getHeroes() { return heroes; }
    @JsonProperty("heroes")
    public void setHeroes(Hero[] value) { this.heroes = value; }

    @JsonProperty("spells")
    public Hero[] getSpells() { return spells; }
    @JsonProperty("spells")
    public void setSpells(Hero[] value) { this.spells = value; }
}
