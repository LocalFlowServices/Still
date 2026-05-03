package com.localflowservices.still.fabric;

import com.localflowservices.still.StillMod;
import net.fabricmc.api.ModInitializer;

public final class StillFabric implements ModInitializer {
    @Override
    public void onInitialize() {
        StillMod.init();
    }
}
