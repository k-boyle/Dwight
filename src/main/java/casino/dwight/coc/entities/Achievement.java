package casino.dwight.coc.entities;

import com.fasterxml.jackson.annotation.JsonProperty;

public class Achievement {
    private String name;
    private long stars;
    private long value;
    private long target;
    private String info;
    private String completionInfo;
    private Village village;

    @JsonProperty("name")
    public String getName() { return name; }
    @JsonProperty("name")
    public void setName(String value) { this.name = value; }

    @JsonProperty("stars")
    public long getStars() { return stars; }
    @JsonProperty("stars")
    public void setStars(long value) { this.stars = value; }

    @JsonProperty("value")
    public long getValue() { return value; }
    @JsonProperty("value")
    public void setValue(long value) { this.value = value; }

    @JsonProperty("target")
    public long getTarget() { return target; }
    @JsonProperty("target")
    public void setTarget(long value) { this.target = value; }

    @JsonProperty("info")
    public String getInfo() { return info; }
    @JsonProperty("info")
    public void setInfo(String value) { this.info = value; }

    @JsonProperty("completionInfo")
    public String getCompletionInfo() { return completionInfo; }
    @JsonProperty("completionInfo")
    public void setCompletionInfo(String value) { this.completionInfo = value; }

    @JsonProperty("village")
    public Village getVillage() { return village; }
    @JsonProperty("village")
    public void setVillage(Village value) { this.village = value; }
}
