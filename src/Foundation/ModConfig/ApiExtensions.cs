using System;
using System.Linq;
using Vintagestory.API.Common;

namespace Foundation.ModConfig
{
    public static class ApiExtensions
    {
        public static TModConfig LoadOrCreateConfig<TModConfig>(this ICoreAPI api, object caller, bool required = false) where TModConfig : ModConfigBase, new()
        {
            var modCode = new TModConfig().ModCode;
            if (string.IsNullOrEmpty(modCode))
                modCode = ModConfigBase.GetModCode(caller);

            return LoadOrCreateConfig<TModConfig>(api, modCode + ".json", required);
        }

        public static TModConfig LoadOrCreateConfig<TModConfig>(this ICoreAPI api, string filename, bool required) where TModConfig : ModConfigBase, new()
        {
            var modCode = filename.Split('.').First();

            try
            {
                var loadedConfig = api.LoadModConfig<TModConfig>(filename);
                if (loadedConfig != null)
                {
                    return loadedConfig;
                }
            }
            catch (Exception e)
            {
                api.World.Logger.Error($"{modCode}: Failed loading modconfig file at 'ModConfig/{filename}', with an error of '{e}'! Stopping...");
                return null;
            }

            var message = $"{modCode}: non-existant modconfig at 'ModConfig/{filename}', creating default" + (required ? " and disabling mod..." : "...");
            api.World.Logger.Notification(message);

            var newConfig = new TModConfig();
            api.StoreModConfig(newConfig, filename);

            return required ? null : newConfig;
        }

        public static void SaveConfig<TModConfig>(this ICoreAPI api, TModConfig config) where TModConfig : ModConfigBase
        {
            var filename = config.ModCode;
            if (string.IsNullOrEmpty(filename))
                filename = ModConfigBase.GetModCode(config);
            if (!filename.EndsWith(".json"))
                filename += ".json";

            api.World.Logger.Notification($"Saving modconfig at 'ModConfig/{filename}'...");

            api.StoreModConfig(config, filename);
        }
    }
}