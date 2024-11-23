using HarmonyLib;
using ProspectTogether.Shared;
using System.Collections.Generic;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace ProspectTogether.Server
{
    [HarmonyPatch(typeof(ModSystemOreMap), "DidProbe")]
    class ModSystemOreMapPatch
    {
        static void Postfix(ModSystemOreMap __instance, PropickReading results, IServerPlayer splr)
        {
            ICoreAPI api = (ICoreAPI)typeof(ModSystemOreMap).GetField("api", BindingFlags.NonPublic |
                     BindingFlags.Instance).GetValue(__instance);

            // Obtain proPickWorkSpace to get page codes
            ProPickWorkSpace proPickWorkSpace = ObjectCacheUtil.TryGet<ProPickWorkSpace>(api, "propickworkspace");

            if (proPickWorkSpace is null)
            {
                // We can't do much without it.
                api.World.Logger.Error("propickworkspace is null");
                return;
            }

            // Convert results to ProspectTogether format
            var occurences = new List<OreOccurence>();
            foreach (var reading in results.OreReadings)
            {
                string pageCode = proPickWorkSpace.pageCodes[reading.Key];
                if (reading.Value.TotalFactor > 0.025)
                {
                    // +2 to offset for our Enum
                    occurences.Add(new OreOccurence("game:ore-" + reading.Key, pageCode, (RelativeDensity)((int)GameMath.Clamp(reading.Value.TotalFactor * 7.5f, 0, 5) + 2), reading.Value.PartsPerThousand));
                }
                else if (reading.Value.TotalFactor > PropickReading.MentionThreshold)
                {
                    occurences.Add(new OreOccurence("game:ore-" + reading.Key, pageCode, RelativeDensity.Miniscule, reading.Value.PartsPerThousand));
                }
            }

            var pos = results.Position;

            // Send information to Player 
            ProspectTogetherModSystem mod = api.ModLoader.GetModSystem<ProspectTogetherModSystem>();
            int chunksize = api.World.BlockAccessor.ChunkSize;
            ProspectInfo info = new(new ChunkCoordinate(pos.XInt / chunksize, pos.ZInt / chunksize), occurences);
            mod.ServerStorage.UserProspected(info, splr);
        }
    }
}
