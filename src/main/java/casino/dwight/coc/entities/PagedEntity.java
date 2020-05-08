package casino.dwight.coc.entities;

import com.fasterxml.jackson.annotation.JsonProperty;

public class PagedEntity<T> {
    private T[] items;
    private Paging paging;

    @JsonProperty("items")
    public T[] getItems() { return items; }
    @JsonProperty("items")
    public void setItems(T[] value) { this.items = value; }

    @JsonProperty("paging")
    public Paging getPaging() { return paging; }
    @JsonProperty("paging")
    public void setPaging(Paging value) { this.paging = value; }

    public static class WarLog extends PagedEntity<casino.dwight.coc.entities.WarLog> {
    }

    public static class Member extends PagedEntity<casino.dwight.coc.entities.Member> {
    }
}
