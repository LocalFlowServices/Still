package com.localflowservices.still;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;

public final class StillMod {
    public static final String MOD_ID = "still";
    public static final Logger LOGGER = LoggerFactory.getLogger(MOD_ID);

    private StillMod() {
    }

    public static void init() {
        LOGGER.info("{} initialized.", MOD_ID);
    }
}
