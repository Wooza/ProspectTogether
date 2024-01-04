using System.Linq;
using Foundation.Extensions;
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

        public virtual void Save(ICoreAPI api)
        {
            api.SaveConfig(this);
        }
    }
}
