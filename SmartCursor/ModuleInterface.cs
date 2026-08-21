using Vintagestory.API.Client;
using Vintagestory.API.Common;
using System.Collections.Generic;

namespace SmartCursor {
public class ModStateManager {
    // High-level state flags to prevent module conflicts
    public bool Lock { get; set; }

    // Lightweight event system so modules can talk without referencing each other directly
    // public event Action<string> OnModuleStateChanged;

    // public void NotifyStateChange(string reason)
    //{
    //     OnModuleStateChanged?.Invoke(reason);
    // }
}

public interface IModModule {
    void Initialize(ICoreClientAPI capi, ModStateManager stateManager);
    // void Dispose();
}

public class GlobalSystem : ModSystem {
    private ICoreClientAPI _capi;
    private ModStateManager _stateManager;
    private List<IModModule> _modules = new List<IModModule>();

    public override bool ShouldLoad(EnumAppSide side) => side == EnumAppSide.Client;

    public override void StartClientSide(ICoreClientAPI api) {
        _capi = api;
        _stateManager = new ModStateManager();

        // Instantiate and initialize modules
        RegisterModule(new SmartCursorModule());
        RegisterModule(new TransferAwayModule());
        // RegisterModule(new QuickSwapModule());
    }

    private void RegisterModule(IModModule module) {
        module.Initialize(_capi, _stateManager);
        _modules.Add(module);
    }

    // public override void Dispose()
    //{
    //     foreach (var module in _modules)
    //     {
    //         module.Dispose();
    //     }
    //     _modules.Clear();
    //     base.Dispose();
    // }
}
}
