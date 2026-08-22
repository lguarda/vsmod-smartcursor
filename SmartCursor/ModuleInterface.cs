using Vintagestory.API.Client;
using Vintagestory.API.Common;
using System.Collections.Generic;
using System;

namespace SmartCursor
{
    public class ModStateManager
    {
        public const string CONFIG_PATH = "smartcursor.json";
        public SmartCursorConfig config;
        ICoreClientAPI capi;

        public ModStateManager(ICoreClientAPI api)
        {
            capi = api;
            LoadConfig(); //  error CS0103: The name 'Loadconfig' does not exist in the current context
        }

        public void SaveConfig()
        {
            capi.StoreModConfig(config, CONFIG_PATH);
        }

        private void LoadConfig()
        {
            try
            {
                config = capi.LoadModConfig<SmartCursorConfig>(CONFIG_PATH);
            }
            catch (Exception)
            {
                config = null;
            }
            if (config == null)
            {
                config = new SmartCursorConfig();
            }
        }

        public bool Lock { get; set; }
    }

    public interface IModModule
    {
        void Initialize(ICoreClientAPI capi, ModStateManager stateManager);
    }

    public class GlobalSystem : ModSystem
    {
        private ICoreClientAPI capi;
        private ModStateManager stateManager;
        private List<IModModule> modules = new List<IModModule>();

        public override bool ShouldLoad(EnumAppSide side) => side == EnumAppSide.Client;

        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;
            capi.Logger.Notification("SmartCursor starting");

            stateManager = new ModStateManager(capi);

            RegisterModule(new SmartCursorModule());
            RegisterModule(new TransferAwayModule());
            RegisterModule(new RefillModule());
        }

        private void RegisterModule(IModModule module)
        {
            module.Initialize(capi, stateManager);
            modules.Add(module);
        }
    }
}
