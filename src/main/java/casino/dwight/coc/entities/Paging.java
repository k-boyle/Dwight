package casino.dwight.coc.entities;

import com.fasterxml.jackson.annotation.JsonProperty;

public class Paging {
    private Cursors cursors;

    @JsonProperty("cursors")
    public Cursors getCursors() { return cursors; }
    @JsonProperty("cursors")
    public void setCursors(Cursors value) { this.cursors = value; }
}
