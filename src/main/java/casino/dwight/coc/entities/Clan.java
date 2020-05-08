package casino.dwight.coc.entities;

import com.fasterxml.jackson.annotation.JsonProperty;

public class Clan {
    private String tag;
    private String name;
    private BadgeUrls badgeUrls;
    private long clanLevel;
    private long attacks;
    private long stars;
    private long destructionPercentage;
    private Long expEarned;
    private Member[] members;

    @JsonProperty("tag")
    public String getTag() { return tag; }
    @JsonProperty("tag")
    public void setTag(String value) { this.tag = value; }

    @JsonProperty("name")
    public String getName() { return name; }
    @JsonProperty("name")
    public void setName(String value) { this.name = value; }

    @JsonProperty("badgeUrls")
    public BadgeUrls getBadgeUrls() { return badgeUrls; }
    @JsonProperty("badgeUrls")
    public void setBadgeUrls(BadgeUrls value) { this.badgeUrls = value; }

    @JsonProperty("clanLevel")
    public long getClanLevel() { return clanLevel; }
    @JsonProperty("clanLevel")
    public void setClanLevel(long value) { this.clanLevel = value; }

    @JsonProperty("attacks")
    public long getAttacks() { return attacks; }
    @JsonProperty("attacks")
    public void setAttacks(long value) { this.attacks = value; }

    @JsonProperty("stars")
    public long getStars() { return stars; }
    @JsonProperty("stars")
    public void setStars(long value) { this.stars = value; }

    @JsonProperty("destructionPercentage")
    public long getDestructionPercentage() { return destructionPercentage; }
    @JsonProperty("destructionPercentage")
    public void setDestructionPercentage(long value) { this.destructionPercentage = value; }

    @JsonProperty("members")
    public Member[] getMembers() { return members; }
    @JsonProperty("members")
    public void setMembers(Member[] value) { this.members = value; }

    @JsonProperty("expEarned")
    public Long getExpEarned() { return expEarned; }
    @JsonProperty("expEarned")
    public void setExpEarned(Long value) { this.expEarned = value; }
}
