package casino.dwight.coc.entities;

import com.fasterxml.jackson.annotation.JsonProperty;

public class CurrentWar {
    private String state;
    private long teamSize;
    private String preparationStartTime;
    private String startTime;
    private String endTime;
    private Clan clan;
    private Clan opponent;

    @JsonProperty("state")
    public String getState() { return state; }
    @JsonProperty("state")
    public void setState(String value) { this.state = value; }

    @JsonProperty("teamSize")
    public long getTeamSize() { return teamSize; }
    @JsonProperty("teamSize")
    public void setTeamSize(long value) { this.teamSize = value; }

    @JsonProperty("preparationStartTime")
    public String getPreparationStartTime() { return preparationStartTime; }
    @JsonProperty("preparationStartTime")
    public void setPreparationStartTime(String value) { this.preparationStartTime = value; }

    @JsonProperty("startTime")
    public String getStartTime() { return startTime; }
    @JsonProperty("startTime")
    public void setStartTime(String value) { this.startTime = value; }

    @JsonProperty("endTime")
    public String getEndTime() { return endTime; }
    @JsonProperty("endTime")
    public void setEndTime(String value) { this.endTime = value; }

    @JsonProperty("clan")
    public Clan getClan() { return clan; }
    @JsonProperty("clan")
    public void setClan(Clan value) { this.clan = value; }

    @JsonProperty("opponent")
    public Clan getOpponent() { return opponent; }
    @JsonProperty("opponent")
    public void setOpponent(Clan value) { this.opponent = value; }
}
