package casino.dwight;

import casino.dwight.coc.CoClient;
import casino.dwight.commands.PlayerSearchCommand;
import casino.dwight.config.ConfigFactory;
import casino.dwight.config.DwightConfig;
import de.btobastian.sdcf4j.handler.JavacordHandler;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;
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
            .login()
            .join();

        dwight.updateActivity("Assistant to The Casino");

        var cocClient = new CoClient(config.getString(DwightConfig.CoC.TOKEN));
        var commandHandler = new JavacordHandler(dwight);
        commandHandler.registerCommand(new PlayerSearchCommand(cocClient));
    }
}
