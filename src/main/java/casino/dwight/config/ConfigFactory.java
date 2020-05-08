package casino.dwight.config;

import com.google.common.collect.ImmutableList;
import com.google.common.collect.ImmutableSet;
import com.ocadotechnology.config.Config;
import com.ocadotechnology.config.ConfigManager;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;

public class ConfigFactory {
    private static final String CONFIG_FILE = "dwight.properties";

    private final Logger logger = LogManager.getLogger(getClass());

    public Config<DwightConfig> create(String[] args) {
        var resources = ImmutableList.of(CONFIG_FILE);
        var configKeys = ImmutableSet.<Class<? extends Enum<?>>>of(DwightConfig.class);
        try {
            var configManager = new ConfigManager.Builder(args)
                    .loadConfigFromLocalResources(resources, configKeys)
                    .build();
            return configManager.getConfig(DwightConfig.class);
        } catch (Exception ex) {
            logger.error("Error when trying to create config", ex);
            System.exit(1);
            return null;
        }
    }
}
