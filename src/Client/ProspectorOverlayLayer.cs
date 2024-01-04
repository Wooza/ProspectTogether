using HarmonyLib;
using ProspectTogether.Shared;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace ProspectTogether.Client
{
    internal class ProspectorOverlayLayer : MapLayer
    {
        private readonly int Chunksize;
        private readonly ClientStorage Storage;
        private readonly ICoreClientAPI ClientApi;
        private readonly Dictionary<ChunkCoordinate, ProspectorOverlayMapComponent> _components = new();
        private readonly IWorldMapManager WorldMapManager;
        private readonly LoadedTexture[] ColorTextures = new LoadedTexture[8];
        private bool TemporaryRenderOverride = false;
        private static ClientModConfig Config;
        private static GuiDialog SettingsDialog;

        public override string Title => "ProspectTogether";
        public override EnumMapAppSide DataSide => EnumMapAppSide.Client;

        public ProspectorOverlayLayer(ICoreAPI api, IWorldMapManager mapSink) : base(api, mapSink)
        {
            WorldMapManager = mapSink;
            Chunksize = api.World.BlockAccessor.ChunkSize;

            var modSystem = api.ModLoader.GetModSystem<ProspectTogetherModSystem>();
            Config = modSystem.ClientConfig;
            Storage = modSystem.ClientStorage;
            Storage.OnChanged += UpdateMapComponents;

            if (api.Side == EnumAppSide.Client)
            {
                ClientApi = (ICoreClientAPI)api;
                ClientApi.Event.AfterActiveSlotChanged += Event_AfterActiveSlotChanged;
                ClientApi.Event.PlayerJoin += (p) =>
                {
                    if (p == ClientApi?.World.Player)
                    {
                        var invMan = p?.InventoryManager?.GetHotbarInventory();
                        invMan.SlotModified += Event_SlotModified;
                    }
                };

                ClientApi.ChatCommands.Create("pt")
                    .WithDescription("ProspectorTogether main command. Defaults to toggling the map overlay.")
                    .HandleWith(OnShowOverlayCommand)
                    .BeginSubCommand("showoverlay")
                        .WithDescription(".pt showoverlay [bool] - Shows or hides the overlay. No argument toggles instead.")
                        .WithArgs(api.ChatCommands.Parsers.OptionalBool("show"))
                        .HandleWith(OnShowOverlayCommand)
                    .EndSubCommand()
                    .BeginSubCommand("showgui")
                        .WithDescription(".pt showgui [bool] - Shows or hides the gui whenever the map is open. No argument toggles instead.")
                        .WithArgs(api.ChatCommands.Parsers.OptionalBool("show"))
                        .HandleWith(OnShowGuiCommand)
                    .EndSubCommand()
                    .BeginSubCommand("showborder")
                        .WithDescription(".pt showborder [bool] - Shows or hides the tile border. No argument toggles instead.<br/>" +
                                         "Sets the \"RenderBorder\" config option (default = true)")
                        .WithArgs(api.ChatCommands.Parsers.OptionalBool("show"))
                        .HandleWith(OnShowBorderCommand)
                    .EndSubCommand()
                    .BeginSubCommand("setcolor")
                        .WithDescription(".pt setcolor (overlay|border|zeroheat|lowheat|highheat) [0-255] [0-255] [0-255] [0-255]<br/>" +
                                         "Sets the given color for the specified element.<br/>" +
                                         "You can specify a color either as RGBA, RGB or only A.<br/>" +
                                         "The lowheat and highheat colors will be blended on the heatmap based on relative density.<br/>" +
                                         "Available elements and corresponding config option:<br/>" +
                                         "overlay: TextureColor (default = 150 125 150 128)<br/>" +
                                         "border: BorderColor (default = 0 0 0 200)<br/>" +
                                         "zeroheat: BorderColor (default = 0 0 0 0)<br/>" +
                                         "lowheat: LowHeatColor (default = 85 85 181 128)<br/>" +
                                         "highheat: HighHeatColor (default = 168 34 36 128)")
                        .WithArgs(api.ChatCommands.Parsers.WordRange("element", "overlay", "border", "lowheat", "highheat"),
                                  new ColorWithAlphaArgParser("color", true))
                        .HandleWith(OnSetColorCommand)
                    .EndSubCommand()
                    .BeginSubCommand("setborderthickness")
                        .WithDescription(".pi setborderthickness [1-5] - Sets the tile outline's thickness.<br/>" +
                                         "Sets the \"BorderThickness\" config option (default = 1)")
                        .WithArgs(api.ChatCommands.Parsers.IntRange("thickness", 1, 5))
                        .HandleWith(OnSetBorderThicknessCommand)
                    .EndSubCommand()
                    .BeginSubCommand("mode")
                        .WithDescription(".pt mode [0-1] - Sets the map mode<br/>" +
                                         "Supported modes: 0 (Default) and 1 (Heatmap)")
                        .WithArgs(api.ChatCommands.Parsers.IntRange("mode", 0, 1))
                        .HandleWith(OnSetModeCommand)
                    .EndSubCommand()
                    .BeginSubCommand("heatmapore")
                        .WithDescription(".pt heatmapore [oreName] - Changes the heatmap mode to display a specific ore<br/>" +
                                         "No argument resets the heatmap back to all ores. Can only handle the ore name in your selected language or the ore tag.<br/>" +
                                         "E.g. game:ore-emerald, game:ore-bituminouscoal, Cassiterite")
                        .WithArgs(api.ChatCommands.Parsers.OptionalWord("oreName"))
                        .HandleWith(OnHeatmapOreCommand)
                    .EndSubCommand()
                    .BeginSubCommand("autoshare")
                        .WithDescription(".pt autoshare [bool] - Automatically share prospecting data")
                        .WithArgs(api.ChatCommands.Parsers.OptionalBool("autoshare"))
                        .HandleWith(OnSetAutoShare)
                    .EndSubCommand()
                    .BeginSubCommand("sendall")
                        .WithDescription(".pt sendall - Send all prospecting data to the server.")
                        .HandleWith(OnSendAll)
                    .EndSubCommand();

                for (int i = 0; i < ColorTextures.Length; i++)
                {
                    ColorTextures[i]?.Dispose();
                    ColorTextures[i] = GenerateOverlayTexture((RelativeDensity)i);
                }

                SettingsDialog = new ProspectTogetherSettingsDialog(ClientApi, Config, RebuildMap, Storage);
                ClientApi.Input.SetHotKeyHandler(Constants.TOGGLE_GUI_HOTKEY_CODE, OnToggleGuiHotkey);
            }
        }

        #region Handling Prospecting Data

        public void UpdateMapComponents(ICollection<ProspectInfo> information)
        {
            foreach (ProspectInfo info in information)
            {
                var newComponent = new ProspectorOverlayMapComponent(ClientApi, info.Chunk, info.GetMessage(), ColorTextures[(int)GetRelativeDensity(info)]);
                _components[info.Chunk] = newComponent;
            }
        }
        #endregion

        #region Commands/Events

        private TextCommandResult OnShowOverlayCommand(TextCommandCallingArgs args)
        {
            // Parser count is 0 when only calling .pi command.
            if (args.Parsers.Count == 0 || args.Parsers[0].IsMissing)
                Config.RenderTexturesOnMap = !Config.RenderTexturesOnMap;
            else
                Config.RenderTexturesOnMap = (bool)args.Parsers[0].GetValue();
            Config.Save(api);
            RebuildMap();
            return TextCommandResult.Success($"Set RenderTexturesOnMap to {Config.RenderTexturesOnMap}.");
        }

        private TextCommandResult OnShowGuiCommand(TextCommandCallingArgs args)
        {
            if (args.Parsers[0].IsMissing)
                Config.ShowGui = !Config.ShowGui;
            else
                Config.ShowGui = (bool)args.Parsers[0].GetValue();
            Config.Save(api);
            SettingsDialog.TryOpen();
            return TextCommandResult.Success($"Set ShowGui to {Config.ShowGui}.");
        }

        private bool OnToggleGuiHotkey(KeyCombination t1)
        {

            if (SettingsDialog.IsOpened())
            {
                SettingsDialog.TryClose();
            }
            else
            {
                SettingsDialog.TryOpen();
            }
            Config.ShowGui = SettingsDialog.IsOpened();
            Config.Save(api);
            return true;
        }

        private TextCommandResult OnSetColorCommand(TextCommandCallingArgs args)
        {
            ColorWithAlphaUpdate colorUpdate = (ColorWithAlphaUpdate)args.Parsers[1].GetValue();
            string changedColorSetting;
            switch ((string)args.Parsers[0].GetValue())
            {
                case "overlay":
                    colorUpdate.ApplyUpdateTo(Config.TextureColor);
                    changedColorSetting = "TextureColor";
                    break;
                case "border":
                    colorUpdate.ApplyUpdateTo(Config.BorderColor);
                    changedColorSetting = "BorderColor";
                    break;
                case "zeroheat":
                    colorUpdate.ApplyUpdateTo(Config.ZeroHeatColor);
                    changedColorSetting = "ZeroHeatColor";
                    break;
                case "lowheat":
                    colorUpdate.ApplyUpdateTo(Config.LowHeatColor);
                    changedColorSetting = "LowHeatColor";
                    break;
                case "highheat":
                    colorUpdate.ApplyUpdateTo(Config.HighHeatColor);
                    changedColorSetting = "HighHeatColor";
                    break;
                default:
                    return TextCommandResult.Error("Unknown element to set color for.");
            }
            Config.Save(api);
            RebuildMap(true);
            return TextCommandResult.Success($"Updated color for {changedColorSetting}.");
        }

        private TextCommandResult OnSetBorderThicknessCommand(TextCommandCallingArgs args)
        {
            var newThickness = (int)args.Parsers[0].GetValue();
            Config.BorderThickness = newThickness;
            Config.Save(api);
            RebuildMap(true);
            return TextCommandResult.Success($"Set BorderThickness to {Config.BorderThickness}.");
        }

        private TextCommandResult OnShowBorderCommand(TextCommandCallingArgs args)
        {
            if (args.Parsers[0].IsMissing)
                Config.RenderBorder = !Config.RenderBorder;
            else
                Config.RenderBorder = (bool)args.Parsers[0].GetValue();
            Config.Save(api);

            RebuildMap(true);
            return TextCommandResult.Success($"Set RenderBorder to {Config.RenderBorder}.");
        }

        private TextCommandResult OnSetModeCommand(TextCommandCallingArgs args)
        {
            var newMode = (int)args.Parsers[0].GetValue();
            Config.MapMode = (MapMode)newMode;
            Config.Save(api);

            RebuildMap(true);
            return TextCommandResult.Success($"Set MapMode to {Config.MapMode}.");
        }

        private TextCommandResult OnHeatmapOreCommand(TextCommandCallingArgs args)
        {
            if (args.Parsers[0].IsMissing)
                Config.HeatMapOre = null;
            else
                Config.HeatMapOre = (string)args.Parsers[0].GetValue();
            Config.Save(api);

            RebuildMap(true);
            return TextCommandResult.Success($"Set HeatMapOre to {Config.HeatMapOre}.");
        }

        private TextCommandResult OnSetAutoShare(TextCommandCallingArgs args)
        {
            if (args.Parsers[0].IsMissing)
                Config.AutoShare = !Config.AutoShare;
            else
                Config.AutoShare = (bool)args.Parsers[0].GetValue();
            Config.Save(api);
            return TextCommandResult.Success($"Set AutoShare to {Config.AutoShare}.");
        }

        private TextCommandResult OnSendAll(TextCommandCallingArgs args)
        {
            Storage.SendAll();
            return TextCommandResult.Success($"Sent all prospecting data to server.");
        }

        private void Event_SlotModified(int slotId)
        {
            UpdateRenderOverride();
        }
        private void Event_AfterActiveSlotChanged(ActiveSlotChangeEventArgs t1)
        {
            UpdateRenderOverride();
        }

        private void UpdateRenderOverride()
        {
            if (!Config.AutoToggle)
                return;

            TemporaryRenderOverride = ProspectingPickInHand;
        }

        private bool ProspectingPickInHand => ClientApi?.World?.Player?.InventoryManager?.ActiveHotbarSlot?.Itemstack?.Item?.Code?
                .FirstPathPart()?.ToLower().StartsWith("prospectingpick") ?? false;

        public override string LayerGroupCode => "prospect-together";

        #endregion

        #region Texture
        private LoadedTexture GenerateOverlayTexture(RelativeDensity? relativeDensity)
        {
            var colorTexture = new LoadedTexture(ClientApi, 0, Chunksize, Chunksize);
            int[] colorArray;
            if (Config.MapMode == MapMode.Heatmap)
            {
                int color;
                if (relativeDensity == RelativeDensity.Zero)
                {
                    color = Config.ZeroHeatColor.RGBA;
                }
                else
                {
                    color = ColorUtil.ColorOverlay(Config.LowHeatColor.RGBA, Config.HighHeatColor.RGBA, 1 * ((int)relativeDensity - 1) / 7.0f);
                }
                colorArray = Enumerable.Repeat(color, Chunksize * Chunksize).ToArray();
            }
            else
            {
                colorArray = Enumerable.Repeat(Config.TextureColor.RGBA, Chunksize * Chunksize).ToArray();
            }

            if (Config.RenderBorder)
            {
                for (int x = 0; x < Chunksize; x++)
                {
                    for (int y = 0; y < Chunksize; y++)
                    {
                        if (x < Config.BorderThickness || x > Chunksize - 1 - Config.BorderThickness)
                            colorArray[y * Chunksize + x] = ColorUtil.ColorOver(colorArray[y * Chunksize + x], Config.BorderColor.RGBA);
                        else if (y < Config.BorderThickness || y > Chunksize - 1 - Config.BorderThickness)
                            colorArray[y * Chunksize + x] = ColorUtil.ColorOver(colorArray[y * Chunksize + x], Config.BorderColor.RGBA);
                    }
                }
            }

            ClientApi.Render.LoadOrUpdateTextureFromRgba(colorArray, false, 0, ref colorTexture);
            ClientApi.Render.BindTexture2d(colorTexture.TextureId);

            return colorTexture;
        }

        public bool UserDisabledMapTextures()
        {
            return !Config.RenderTexturesOnMap;
        }
        #endregion

        public override void OnMapOpenedClient()
        {
            if (!WorldMapManager.IsOpened)
                return;

            RebuildMap();
        }

        public void RebuildMap(bool rebuildTexture = false)
        {

            if (rebuildTexture)
            {
                for (int i = 0; i < ColorTextures.Length; i++)
                {
                    ColorTextures[i]?.Dispose();
                    ColorTextures[i] = GenerateOverlayTexture((RelativeDensity)i);
                }
            }

            lock (Storage.Lock)
            {
                _components.Clear();
                UpdateMapComponents(Storage.Data.Values);
            }
        }

        private RelativeDensity GetRelativeDensity(ProspectInfo prospectInfo)
        {
            if (Config.HeatMapOre == null)
                if (prospectInfo.Values != null && prospectInfo.Values.Count > 0)
                    return prospectInfo.Values.First().RelativeDensity;
                else
                    return RelativeDensity.Zero;
            else
                return prospectInfo.GetValueOfOre(Config.HeatMapOre);
        }

        public override void OnMouseMoveClient(MouseEvent args, GuiElementMap mapElem, StringBuilder hoverText)
        {
            lock (Storage.Lock)
            {
                foreach (var component in _components.Values)
                {
                    component.OnMouseMove(args, mapElem, hoverText);
                }
            }
        }

        public override void Render(GuiElementMap mapElem, float dt)
        {
            if (!TemporaryRenderOverride && UserDisabledMapTextures())
                return;

            lock (Storage.Lock)
            {
                foreach (var component in _components.Values)
                {
                    component.Render(mapElem, dt);
                }
            }
        }

        public override void Dispose()
        {
            foreach (var texture in ColorTextures)
            {
                texture?.Dispose();
            }
            base.Dispose();
        }

        [HarmonyPatch(typeof(GuiDialogWorldMap), "TryClose")]
        class GuiDialogWorldMapTryClosePatch
        {
            static void Postfix()
            {
                SettingsDialog.TryClose();
            }
        }

        [HarmonyPatch(typeof(GuiDialogWorldMap), "Open")]
        class GuiDialogWorldMapOpenPatch
        {
            static void Postfix(EnumDialogType type)
            {
                if (Config.ShowGui && type == EnumDialogType.Dialog)
                    SettingsDialog.TryOpen();
                else
                    SettingsDialog.TryClose();
            }
        }


    }
}
