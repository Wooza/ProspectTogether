using Foundation.ModConfig;
using ProspectTogether.Shared;
using Vintagestory.API.Common;

namespace ProspectTogether.Client
{


    public enum MapMode
    {
        Default,
        Heatmap
    }

    public class ClientModConfig : CommonConfig
    {
        public override string ModCode => "ProspectTogetherClient";

        public ColorWithAlpha TextureColor { get; set; } = new ColorWithAlpha(150, 125, 150, 128);
        public ColorWithAlpha ZeroHeatColor { get; set; } = new ColorWithAlpha(0, 0, 0, 0);
        public ColorWithAlpha LowHeatColor { get; set; } = new ColorWithAlpha(85, 85, 181, 128);
        public ColorWithAlpha HighHeatColor { get; set; } = new ColorWithAlpha(168, 34, 36, 128);
        public ColorWithAlpha BorderColor { get; set; } = new ColorWithAlpha(0, 0, 0, 200);
        public int BorderThickness { get; set; } = 1;
        public bool RenderBorder { get; set; } = true;
        public MapMode MapMode { get; set; } = MapMode.Default;
        public string HeatMapOre { get; set; } = null;
        public bool AutoShare { get; set; } = false;
        public int ShareGroupUid { get; set; } = Constants.ALL_GROUP_ID;

        public override void Save(ICoreAPI api)
        {
            api.SaveConfig(this, perServer: true);
        }

    }



}
