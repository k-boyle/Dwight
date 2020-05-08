package casino.dwight.coc;

import casino.dwight.coc.entities.Player;

import java.net.URI;
import java.net.http.HttpClient;
import java.net.http.HttpRequest;
import java.net.http.HttpResponse;
import java.util.concurrent.CompletableFuture;

public class CoClient {
    private static final String BASE_URL = "https://api.clashofclans.com/v1";
    private static final String AUTHORIZATION_HEADER_KEY = "Authorization";

    private final HttpClient client;
    private final JsonDeserialiser deserialiser;
    private final HttpRequest.Builder baseRequest;

    public CoClient(String token) {
        this.client = HttpClient.newHttpClient();
        this.deserialiser = new JsonDeserialiser();
        this.baseRequest = HttpRequest.newBuilder()
            .setHeader(AUTHORIZATION_HEADER_KEY, "Bearer " + token);
    }

    public CompletableFuture<Player> getPlayerAsync(String playerTag) {
        var request = baseRequest
            .uri(URI.create(BASE_URL + "/players/" + playerTag.replace("#", "%23")))
            .build();
        return client.sendAsync(request, HttpResponse.BodyHandlers.ofString())
            .thenApplyAsync(r -> deserialiser.fromJson(Player.class, r.body()));
    }
}
