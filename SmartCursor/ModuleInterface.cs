using Vintagestory.API.Client;
using Vintagestory.API.Common;
using System.Collections.Generic;
using System;

namespace SmartCursor {
public class ModStateManager {
    public const string CONFIG_PATH = "smartcursor.json";
    public SmartCursorConfig config;
    ICoreClientAPI _capi;

    public ModStateManager(ICoreClientAPI capi) {
        _capi = capi;
        LoadConfig(); //  error CS0103: The name 'Loadconfig' does not exist in the current context
    }

    public void SaveConfig() {
        _capi.StoreModConfig(config, CONFIG_PATH);
    }

    private void LoadConfig() {
        try {
            config = _capi.LoadModConfig<SmartCursorConfig>(CONFIG_PATH);
        } catch (Exception) {
            config = null;
        }
        if (config == null) {
            config = new SmartCursorConfig();
        }
    }

    public bool Lock { get; set; }
}

public interface IModModule {
    void Initialize(ICoreClientAPI capi, ModStateManager stateManager);
}

public class GlobalSystem : ModSystem {
    private ICoreClientAPI _capi;
    private ModStateManager _stateManager;
    private List<IModModule> _modules = new List<IModModule>();

    public override bool ShouldLoad(EnumAppSide side) => side == EnumAppSide.Client;

    public override void StartClientSide(ICoreClientAPI api) {
        _capi = api;
        _capi.Logger.Notification("SmartCursor starting");

        _stateManager = new ModStateManager(_capi);

        RegisterModule(new SmartCursorModule());
        RegisterModule(new TransferAwayModule());
        RegisterModule(new RefillModule());
    }

    private void RegisterModule(IModModule module) {
        module.Initialize(_capi, _stateManager);
        _modules.Add(module);
    }
}
}
