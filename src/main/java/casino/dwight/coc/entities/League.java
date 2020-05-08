package casino.dwight.coc.entities;

import com.fasterxml.jackson.annotation.JsonProperty;

public class League {
    private long id;
    private String name;
    private LeagueIconUrls iconUrls;

    @JsonProperty("id")
    public long getId() { return id; }
    @JsonProperty("id")
    public void setId(long value) { this.id = value; }

    @JsonProperty("name")
    public String getName() { return name; }
    @JsonProperty("name")
    public void setName(String value) { this.name = value; }

    @JsonProperty("iconUrls")
    public LeagueIconUrls getIconUrls() { return iconUrls; }
    @JsonProperty("iconUrls")
    public void setIconUrls(LeagueIconUrls value) { this.iconUrls = value; }
}
