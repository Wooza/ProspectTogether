using HarmonyLib;
using ProspectTogether.Shared;
using System;
using System.Collections.Generic;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;

namespace ProspectTogether.Server
{
    [HarmonyPatch(typeof(ItemProspectingPick), "PrintProbeResults")]
    class PrintProbeResultsPatch
    {
        static void Postfix(ItemProspectingPick __instance, IWorldAccessor world, IServerPlayer splr, ItemSlot itemslot, BlockPos pos)
        {
            if (world.Side != EnumAppSide.Server)
                return;

            // Some reflection to get this instance field.
            ProPickWorkSpace ppws = (ProPickWorkSpace)typeof(ItemProspectingPick).GetField("ppws", BindingFlags.NonPublic |
                     BindingFlags.Instance).GetValue(__instance);

            // Code adapted from https://github.com/anegostudios/vssurvivalmod/blob/master/Item/ItemProspectingPick.cs
            DepositVariant[] deposits = world.Api.ModLoader.GetModSystem<GenDeposits>()?.Deposits;
            if (deposits == null) return;

            IBlockAccessor blockAccess = world.BlockAccessor;
            int chunksize = blockAccess.ChunkSize;
            int regsize = blockAccess.RegionSize;

            IMapRegion reg = world.BlockAccessor.GetMapRegion(pos.X / regsize, pos.Z / regsize);
            int lx = pos.X % regsize;
            int lz = pos.Z % regsize;

            pos = pos.Copy();
            pos.Y = world.BlockAccessor.GetTerrainMapheightAt(pos);

            int[] blockColumn = ppws.GetRockColumn(pos.X, pos.Z);

            var occurences = new List<OreOccurence>();

            foreach (var val in reg.OreMaps)
            {
                IntDataMap2D map = val.Value;
                int noiseSize = map.InnerSize;

                float posXInRegionOre = (float)lx / regsize * noiseSize;
                float posZInRegionOre = (float)lz / regsize * noiseSize;

                int oreDist = map.GetUnpaddedColorLerped(posXInRegionOre, posZInRegionOre);

                if (!ppws.depositsByCode.ContainsKey(val.Key))
                {
                    continue;
                }

                ppws.depositsByCode[val.Key].GetPropickReading(pos, oreDist, blockColumn, out double ppt, out double totalFactor);

                string pageCode = ppws.pageCodes[val.Key];
                if (totalFactor > 0.025)
                {
                    occurences.Add(new OreOccurence("game:ore-" + val.Key, pageCode, (RelativeDensity)((int)GameMath.Clamp(totalFactor * 7.5f, 0, 5) + 2), ppt));
                }
                else if (totalFactor > 0.002)
                {
                    occurences.Add(new OreOccurence("game:ore-" + val.Key, pageCode, RelativeDensity.Miniscule, 0));
                }
            }

            ProspectTogetherModSystem mod = world.Api.ModLoader.GetModSystem<ProspectTogetherModSystem>();
            ProspectInfo info = new(new ChunkCoordinate(pos.X / chunksize, pos.Z / chunksize), occurences);
            mod.ServerStorage.UserProspected(info, splr);
        }
    }
}
