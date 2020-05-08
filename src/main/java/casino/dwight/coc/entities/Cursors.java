package casino.dwight.coc.entities;

import com.fasterxml.jackson.annotation.JsonProperty;

public class Cursors {
    private String after;
    private String before;

    @JsonProperty("after")
    public String getAfter() { return after; }
    @JsonProperty("after")
    public void setAfter(String value) { this.after = value; }

    @JsonProperty("before")
    public String getBefore() { return before; }
    @JsonProperty("before")
    public void setBefore(String value) { this.before = value; }
}

