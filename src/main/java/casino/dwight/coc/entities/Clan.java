package casino.dwight.coc.entities;

import com.fasterxml.jackson.annotation.JsonProperty;

public class Clan {
    private String tag;
    private String name;
    private long clanLevel;
    private BadgeUrls badgeUrls;

    @JsonProperty("tag")
    public String getTag() { return tag; }
    @JsonProperty("tag")
    public void setTag(String value) { this.tag = value; }

    @JsonProperty("name")
    public String getName() { return name; }
    @JsonProperty("name")
    public void setName(String value) { this.name = value; }

    @JsonProperty("clanLevel")
    public long getClanLevel() { return clanLevel; }
    @JsonProperty("clanLevel")
    public void setClanLevel(long value) { this.clanLevel = value; }

    @JsonProperty("badgeUrls")
    public BadgeUrls getBadgeUrls() { return badgeUrls; }
    @JsonProperty("badgeUrls")
    public void setBadgeUrls(BadgeUrls value) { this.badgeUrls = value; }
}
