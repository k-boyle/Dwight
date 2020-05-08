package casino.dwight;

import casino.dwight.bootlegdi.BootlegServiceProvider;
import casino.dwight.coc.CoClient;
import casino.dwight.config.ConfigFactory;
import casino.dwight.config.DwightConfig;
import de.btobastian.sdcf4j.CommandExecutor;
import de.btobastian.sdcf4j.handler.JavacordHandler;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;
import org.javacord.api.DiscordApi;
import org.javacord.api.DiscordApiBuilder;
import org.javacord.api.util.logging.ExceptionLogger;

public class DwightController {
    private final Logger logger = LogManager.getLogger(getClass());

    public static void main(String[] args) {
        Thread.setDefaultUncaughtExceptionHandler(ExceptionLogger.getUncaughtExceptionHandler());
        var dwight = new DwightController();
        dwight.run(args);
    }

    private void run(String[] args) {
        logger.info("Starting Dwight...");
        var configFactory = new ConfigFactory();
        var config = configFactory.create(args);
        var dwight = new DiscordApiBuilder()
            .setToken(config.getString(DwightConfig.Discord.TOKEN))
            .addServerBecomesAvailableListener(event -> {
                logger.info("{} has became available", event.getServer().getName());
            })
            .addLostConnectionListener(event -> {
                logger.info("Disconnected");
            })
            .addReconnectListener(event -> {
                logger.info("Connected");
            })
            .login()
            .join();

        var cocClient = new CoClient(config.getString(DwightConfig.CoC.TOKEN));
        var services = new BootlegServiceProvider()
            .add(cocClient)
            .add(DiscordApi.class, dwight)
            .add(JavacordHandler.class)
            .addAll(CommandExecutor.class);

        var commandHandler = services.get(JavacordHandler.class);
        commandHandler.setDefaultPrefix(">");
        services.getAll(CommandExecutor.class).forEach(commandHandler::registerCommand);
    }
}
