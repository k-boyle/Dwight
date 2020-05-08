package casino.dwight.coc.entities;

import com.fasterxml.jackson.annotation.JsonProperty;

public class Hero {
    private String name;
    private long level;
    private long maxLevel;
    private Village village;

    @JsonProperty("name")
    public String getName() { return name; }
    @JsonProperty("name")
    public void setName(String value) { this.name = value; }

    @JsonProperty("level")
    public long getLevel() { return level; }
    @JsonProperty("level")
    public void setLevel(long value) { this.level = value; }

    @JsonProperty("maxLevel")
    public long getMaxLevel() { return maxLevel; }
    @JsonProperty("maxLevel")
    public void setMaxLevel(long value) { this.maxLevel = value; }

    @JsonProperty("village")
    public Village getVillage() { return village; }
    @JsonProperty("village")
    public void setVillage(Village value) { this.village = value; }
}
