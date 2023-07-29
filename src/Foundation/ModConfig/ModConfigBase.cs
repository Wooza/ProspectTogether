using System.Linq;
using Vintagestory.API.Common;

namespace Foundation.ModConfig
{
    public abstract class ModConfigBase
    {
        public abstract string ModCode { get; }

        public static string GetModCode(object caller)
        {
            return caller.GetType().Namespace.Split('.').FirstOrDefault() ?? "unknown-mod-code";
        }

        public void Save(ICoreAPI api, string filename = default)
        {
            if (string.IsNullOrEmpty(filename))
                filename = this.ModCode;
            if (string.IsNullOrEmpty(filename))
                filename = ModConfigBase.GetModCode(this);
            if (!filename.EndsWith(".json"))
                filename += ".json";

            api.World.Logger.Notification($"Saving modconfig at 'ModConfig/{filename}'...");

            api.StoreModConfig(this, filename);
        }
    }
}
