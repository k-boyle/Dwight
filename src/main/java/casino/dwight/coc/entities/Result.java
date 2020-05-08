package casino.dwight.coc.entities;

import com.fasterxml.jackson.annotation.JsonCreator;
import com.fasterxml.jackson.annotation.JsonValue;

import java.io.IOException;

public enum Result {
    LOSE, WIN;

    @JsonValue
    public String toValue() {
        return switch (this) {
            case LOSE -> "lose";
            case WIN -> "win";
        };
    }

    @JsonCreator
    public static Result forValue(String value) throws IOException {
        if (value.equals("lose")) return LOSE;
        if (value.equals("win")) return WIN;
        throw new IOException("Cannot deserialize Result");
    }
}
