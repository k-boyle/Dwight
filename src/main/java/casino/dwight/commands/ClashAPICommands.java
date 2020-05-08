package casino.dwight.commands;

import casino.dwight.coc.CoClient;
import de.btobastian.sdcf4j.Command;
import de.btobastian.sdcf4j.CommandExecutor;
import org.javacord.api.entity.channel.ServerTextChannel;

import java.util.concurrent.ExecutionException;

public class ClashAPICommands implements CommandExecutor {
    private final CoClient client;

    public ClashAPICommands(CoClient client) {
        this.client = client;
    }

    @Command(aliases = "player")
    public void playerSearch(ServerTextChannel channel, String cmd, String playerTag) throws ExecutionException, InterruptedException {
        var player = client.getPlayerAsync(playerTag).get();
        channel.sendMessage(player.getName());
    }

    @Command(aliases = "currentwar")
    public void currentWar(ServerTextChannel channel) throws ExecutionException, InterruptedException {
        var war = client.getCurrentWarAsync("#2GGCRC90").get();
        channel.sendMessage(war.getOpponent().getName());
    }

    @Command(aliases = "warlog")
    public void warLog(ServerTextChannel channel) throws ExecutionException, InterruptedException {
        var warLog = client.getWarLogAsync("#2GGCRC90", 10).get();
        channel.sendMessage(String.valueOf(warLog.length));
    }

    @Command(aliases = "members")
    public void members(ServerTextChannel channel) throws ExecutionException, InterruptedException {
        var members = client.getMembersAsync("#2GGCRC90").get();
        channel.sendMessage(String.valueOf(members.length));
    }
}
