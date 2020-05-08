package casino.dwight.commands;

import casino.dwight.coc.CoClient;
import de.btobastian.sdcf4j.Command;
import de.btobastian.sdcf4j.CommandExecutor;
import org.javacord.api.entity.channel.ServerTextChannel;

import java.util.concurrent.ExecutionException;

public class PlayerSearchCommand implements CommandExecutor {
    private final CoClient client;

    public PlayerSearchCommand(CoClient client) {
        this.client = client;
    }

    @Command(aliases = "player")
    public void playerSearch(ServerTextChannel channel, String cmd, String playerTag) throws ExecutionException, InterruptedException {
        var player = client.getPlayerAsync(playerTag).get();
        channel.sendMessage(player.getName());
    }
}
