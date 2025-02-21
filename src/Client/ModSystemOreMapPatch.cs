using HarmonyLib;
using ProspectTogether.Shared;
using System.Collections.Generic;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace ProspectTogether.Client
{
    [HarmonyPatch]
    class OreMapLayerPatch
    {

        [HarmonyPrefix]
        [HarmonyPatch(typeof(OreMapLayer), nameof(OreMapLayer.OnDataFromServer))]
        static void OnDataFromServer(OreMapLayer __instance, byte[] data)
        {
            ICoreClientAPI capi = (ICoreClientAPI)typeof(OreMapLayer).GetField("capi", BindingFlags.NonPublic |
                     BindingFlags.Instance).GetValue(__instance);

            // Obtain pageCodes
            var pageCodes = capi.ModLoader.GetModSystem<ModSystemOreMap>().prospectingMetaData.PageCodes;

            if (pageCodes is null)
            {
                // We can't do much without it.
                capi.World.Logger.Error("pageCodes is null");
                return;
            }

            ProspectTogetherModSystem mod = capi.ModLoader.GetModSystem<ProspectTogetherModSystem>();
            if (mod is null)
            {
                // Mod not loaded?
                return;
            }

            List<PropickReading> results = SerializerUtil.Deserialize<List<PropickReading>>(data);

            foreach (var result in results)
            {
                // Convert results to ProspectTogether format
                var occurences = new List<OreOccurence>();
                foreach (var reading in result.OreReadings)
                {
                    string pageCode = reading.Key;
                    if (pageCodes.ContainsKey(reading.Key))
                    {
                        pageCode = pageCodes.Get(reading.Key);
                    }

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

                var pos = result.Position;

                // Send data to mod
                int chunksize = GlobalConstants.ChunkSize;
                ProspectInfo info = new(new ChunkCoordinate(pos.XInt / chunksize, pos.ZInt / chunksize), occurences);
                mod.ClientStorage.PlayerProspected(info);
            }
        }
    }
}
