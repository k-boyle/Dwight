package casino.dwight.coc.entities;

import com.fasterxml.jackson.annotation.JsonProperty;

public class WarLog {
    private Result result;
    private String endTime;
    private long teamSize;
    private Clan clan;
    private Clan opponent;

    @JsonProperty("result")
    public Result getResult() { return result; }
    @JsonProperty("result")
    public void setResult(Result value) { this.result = value; }

    @JsonProperty("endTime")
    public String getEndTime() { return endTime; }
    @JsonProperty("endTime")
    public void setEndTime(String value) { this.endTime = value; }

    @JsonProperty("teamSize")
    public long getTeamSize() { return teamSize; }
    @JsonProperty("teamSize")
    public void setTeamSize(long value) { this.teamSize = value; }

    @JsonProperty("clan")
    public Clan getClan() { return clan; }
    @JsonProperty("clan")
    public void setClan(Clan value) { this.clan = value; }

    @JsonProperty("opponent")
    public Clan getOpponent() { return opponent; }
    @JsonProperty("opponent")
    public void setOpponent(Clan value) { this.opponent = value; }
}
