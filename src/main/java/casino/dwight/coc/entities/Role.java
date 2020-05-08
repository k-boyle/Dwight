package casino.dwight.coc.entities;

import com.fasterxml.jackson.annotation.JsonCreator;
import com.fasterxml.jackson.annotation.JsonValue;

import java.io.IOException;

public enum Role {
    ADMIN, CO_LEADER, LEADER, MEMBER;

    @JsonValue
    public String toValue() {
        return switch (this) {
            case ADMIN -> "admin";
            case CO_LEADER -> "coLeader";
            case LEADER -> "leader";
            case MEMBER -> "member";
        };
    }

    @JsonCreator
    public static Role forValue(String value) throws IOException {
        if (value.equals("admin")) return ADMIN;
        if (value.equals("coLeader")) return CO_LEADER;
        if (value.equals("leader")) return LEADER;
        if (value.equals("member")) return MEMBER;
        throw new IOException("Cannot deserialize Role");
    }
}
