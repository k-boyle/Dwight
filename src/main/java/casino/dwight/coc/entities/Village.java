package casino.dwight.coc.entities;

import com.fasterxml.jackson.annotation.JsonCreator;
import com.fasterxml.jackson.annotation.JsonValue;

import java.io.IOException;

public enum Village {
    BUILDER_BASE, HOME;

    @JsonValue
    public String toValue() {
        return switch (this) {
            case BUILDER_BASE -> "builderBase";
            case HOME -> "home";
        };
    }

    @JsonCreator
    public static Village forValue(String value) throws IOException {
        if (value.equals("builderBase")) return BUILDER_BASE;
        if (value.equals("home")) return HOME;
        throw new IOException("Cannot deserialize Village");
    }
}
