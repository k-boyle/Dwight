package casino.dwight.coc;

import casino.dwight.coc.entities.CurrentWar;
import casino.dwight.coc.entities.Member;
import casino.dwight.coc.entities.PagedEntity;
import casino.dwight.coc.entities.Player;
import casino.dwight.coc.entities.WarLog;

import java.net.URI;
import java.net.http.HttpClient;
import java.net.http.HttpRequest;
import java.net.http.HttpResponse;
import java.util.concurrent.CompletableFuture;
import java.util.function.Function;

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
        return getAsync("/players/" + playerTag.replace("#", "%23"),
            r -> deserialiser.fromJson(Player.class, r.body()));
    }

    public CompletableFuture<CurrentWar> getCurrentWarAsync(String clanTag) {
        return getAsync("/clans/" + clanTag.replace("#", "%23") + "/currentwar",
            r -> deserialiser.fromJson(CurrentWar.class, r.body()));
    }

    public CompletableFuture<WarLog[]> getWarLogAsync(String clanTag, int limit) {
        return getAsync("/clans/" + clanTag.replace("#", "%23") + "/warlog?limit=" + limit,
            r -> deserialiser.fromJson(PagedEntity.WarLog.class, r.body()).getItems());
    }

    public CompletableFuture<Member[]> getMembersAsync(String clanTag) {
        return getAsync("/clans/" + clanTag.replace("#", "%23") + "/members",
            r -> deserialiser.fromJson(PagedEntity.Member.class, r.body()).getItems());
    }

    private <T> CompletableFuture<T> getAsync(String endpoint, Function<HttpResponse<String>, T> function) {
        return client.sendAsync(baseRequest.uri(URI.create(BASE_URL + endpoint)).build(), HttpResponse.BodyHandlers.ofString())
            .thenApplyAsync(function);
    }
}
