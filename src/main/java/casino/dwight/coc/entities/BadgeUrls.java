package casino.dwight.coc.entities;

import com.fasterxml.jackson.annotation.JsonProperty;

public class BadgeUrls {
    private String small;
    private String large;
    private String medium;

    @JsonProperty("small")
    public String getSmall() { return small; }
    @JsonProperty("small")
    public void setSmall(String value) { this.small = value; }

    @JsonProperty("large")
    public String getLarge() { return large; }
    @JsonProperty("large")
    public void setLarge(String value) { this.large = value; }

    @JsonProperty("medium")
    public String getMedium() { return medium; }
    @JsonProperty("medium")
    public void setMedium(String value) { this.medium = value; }
}
