using Vintagestory.API.Client;
using Vintagestory.API.Common;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Linq;

namespace SmartCursor
{

    [AttributeUsage(AttributeTargets.Class)]
    public class ModModuleAttribute : Attribute { }

    public class ModStateManager
    {
        public const string CONFIG_PATH = "smartcursor.json";
        public SmartCursorConfig config;
        ICoreClientAPI capi;
        public bool lockInv;
        public long unlockAt;

        public ModStateManager(ICoreClientAPI api)
        {
            capi = api;
            LoadConfig();
            lockInv = false;
            unlockAt = 0;
        }

        public void SaveConfig() { capi.StoreModConfig(config, CONFIG_PATH); }

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

        public void Lock()
        {
            lockInv = true;
        }
        public void Unlock()
        {
            unlockAt = capi.World.ElapsedMilliseconds;
            lockInv = false;
        }
    }

    public interface IModModule
    {
        void Initialize(ICoreClientAPI capi, ModStateManager stateManager);
        void Dispose() { }
    }

    public class GlobalSystem : ModSystem
    {
        private ICoreClientAPI capi;
        private ModStateManager stateManager;
        private List<IModModule> modules;

        public override bool ShouldLoad(EnumAppSide side) => side == EnumAppSide.Client;

        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;
            capi.Logger.Notification("SmartCursor starting");

            stateManager = new ModStateManager(capi);
            LoadModules();
            foreach (var m in modules) m.Initialize(capi, stateManager);
        }

        public void LoadModules()
        {
            var moduleTypes = Assembly.GetExecutingAssembly() // or GetAssemblies() for multiple
        .GetTypes()
        .Where(t => t.IsClass && !t.IsAbstract
            && typeof(IModModule).IsAssignableFrom(t)
            && t.GetCustomAttribute<ModModuleAttribute>() != null);

            modules = moduleTypes
                .Select(t => (IModModule)Activator.CreateInstance(t)!)
                .ToList();
        }

        private void RegisterModule(IModModule module)
        {
            module.Initialize(capi, stateManager);
            modules.Add(module);
        }

        public override void Dispose()
        {
            foreach (var m in modules) m.Dispose();
        }

    }
}
