using HarmonyLib;
using ProspectTogether.Shared;
using System.Collections.Generic;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace ProspectTogether.Server
{
    [HarmonyPatch(typeof(ItemProspectingPick), "PrintProbeResults")]
    class PrintProbeResultsPatch
    {
        static void Postfix(ItemProspectingPick __instance, IWorldAccessor world, IServerPlayer splr, ItemSlot itemslot, BlockPos pos)
        {
            if (world.Side != EnumAppSide.Server)
                return;

            // Some reflection to get access to some protected stuff
            ProPickWorkSpace ppws = (ProPickWorkSpace)typeof(ItemProspectingPick).GetField("ppws", BindingFlags.NonPublic |
                     BindingFlags.Instance).GetValue(__instance);
            MethodInfo GenProbeResultsMethod = typeof(ItemProspectingPick).GetMethod("GenProbeResults", BindingFlags.NonPublic |
                     BindingFlags.Instance);

            PropickReading results = (PropickReading)GenProbeResultsMethod.Invoke(__instance, new object[] { world, pos });
            if (results == null)
            {
                return;
            }

            // Convert results to ProspectTogether format
            var occurences = new List<OreOccurence>();
            foreach (var reading in results.OreReadings)
            {
                string pageCode = ppws.pageCodes[reading.Key];
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

            // Send information to Player 
            ProspectTogetherModSystem mod = world.Api.ModLoader.GetModSystem<ProspectTogetherModSystem>();
            IBlockAccessor blockAccess = world.BlockAccessor;
            int chunksize = blockAccess.ChunkSize;
            ProspectInfo info = new(new ChunkCoordinate(pos.X / chunksize, pos.Z / chunksize), occurences);
            mod.ServerStorage.UserProspected(info, splr);
        }
    }
}
