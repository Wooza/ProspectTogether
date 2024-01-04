using ProspectTogether.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace ProspectTogether.Client
{
    public class ProspectTogetherSettingsDialog : GuiDialog
    {
        public override string ToggleKeyCombinationCode => "prospecttogethersettings";
        private readonly ClientModConfig Config;

        // Callback to update the map
        private readonly Action<bool> RebuildMap;

        // Ores that are displayed in the ore selection
        private List<KeyValuePair<string, string>> Ores;
        private readonly int NumFixedOreEntries;

        // Groups that are displayed in the group selection
        private List<KeyValuePair<string, string>> Groups;
        private readonly int NumFixedGroupEntries;
        private readonly ClientStorage Storage;

        public ProspectTogetherSettingsDialog(ICoreClientAPI capi, ClientModConfig config, Action<bool> rebuildMap, ClientStorage storage) : base(capi)
        {
            Storage = storage;
            Config = config;
            RebuildMap = rebuildMap;

            Ores = new List<KeyValuePair<string, string>>
            {
                new("All ores", null)
            };
            NumFixedOreEntries = Ores.Count;

            Groups = new List<KeyValuePair<string, string>>
            {
                new(ModLang.Get("dialog-all-players"), Constants.ALL_GROUP_ID.ToString()),
                new("Unknown Group", Constants.UNKNOWN_GROUP_ID.ToString())
            };
            NumFixedGroupEntries = Groups.Count;

            SetupDialog();
        }

        private bool UpdateOres()
        {
            HashSet<string> oldOres = Ores.Select((pair) => pair.Value).Skip(NumFixedOreEntries).ToHashSet();
            HashSet<string> newOres = Storage.FoundOres.Select((pair) => pair.Value).ToHashSet();

            if (!oldOres.Equals(newOres))
            {
                if (Ores.Count > NumFixedOreEntries)
                {
                    Ores.RemoveRange(NumFixedOreEntries, Ores.Count - NumFixedOreEntries);
                }
                Ores.AddRange(Storage.FoundOres.OrderBy((pair) => pair.Key).ToList());
                return true;
            }
            return false;
        }

        private bool UpdateGroups()
        {
            HashSet<string> oldGroups = Groups.Select((pair) => pair.Value).Skip(NumFixedGroupEntries).ToHashSet();
            HashSet<string> newGroups = capi.World.Player.Groups.Select((g) => g.GroupUid.ToString()).ToHashSet();

            if (!oldGroups.Equals(newGroups))
            {
                if (Groups.Count > NumFixedGroupEntries)
                {
                    Groups.RemoveRange(NumFixedGroupEntries, Groups.Count - NumFixedGroupEntries);
                }

                foreach (PlayerGroupMembership group in capi.World.Player.Groups)
                {
                    Groups.Add(new KeyValuePair<string, string>(group.GroupName, group.GroupUid.ToString()));
                }
                return true;
            }
            return false;
        }

        public override bool TryOpen()
        {
            lock (Storage.Lock)
            {
                bool setupAgain = false;
                setupAgain |= UpdateOres();
                setupAgain |= UpdateGroups();

                if (setupAgain)
                {
                    SetupDialog();
                }
            }

            return base.TryOpen();
        }

        private void SetupDialog()
        {
            // Auto-sized dialog at the center of the screen
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightMiddle);
            ElementBounds backgroundBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            ElementBounds dialogContainerBounds = ElementBounds.Fixed(0, 40, 200, 220);
            backgroundBounds.BothSizing = ElementSizing.FitToChildren;
            backgroundBounds.WithChildren(dialogContainerBounds);

            ElementBounds showOverlayTextBounds = ElementBounds.Fixed(35, 55, 160, 40);
            ElementBounds switchBounds = ElementBounds.Fixed(170, 50);
            ElementBounds mapModeBounds = ElementBounds.Fixed(35, 90, 160, 20);
            ElementBounds oreBounds = ElementBounds.Fixed(35, 130, 160, 20);
            ElementBounds autoShareTextBounds = ElementBounds.Fixed(35, 165, 160, 40);
            ElementBounds autoShareSwitchBounds = ElementBounds.Fixed(170, 160);
            ElementBounds shareGroupBounds = ElementBounds.Fixed(35, 205, 160, 20);
            ElementBounds sendAllBounds = ElementBounds.Fixed(35, 240, 160, 20);

            var currentHeatmapOreIndex = 0;
            if (Config.HeatMapOre != null)
            {
                currentHeatmapOreIndex = Ores.FindIndex((pair) => pair.Value != null && pair.Value.Contains(Config.HeatMapOre));
                if (currentHeatmapOreIndex == -1) // config.HeatMapOre is not a valid ore name -> reset to all ores
                    currentHeatmapOreIndex = 0;
            }

            int currentGroupIndex;
            if (Config.ShareGroupUid == Constants.ALL_GROUP_ID)
            {
                currentGroupIndex = 0;
            }
            else
            {
                var search = Groups.FindIndex(pair => pair.Value == Config.ShareGroupUid.ToString());
                if (search == -1)
                {
                    // Unknown group
                    currentGroupIndex = 1;
                }
                else
                {
                    // Group Found
                    currentGroupIndex = search;
                }
            }




            SingleComposer = capi.Gui.CreateCompo("ProspectTogether Settings", dialogBounds)
                .AddShadedDialogBG(backgroundBounds)
                .AddDialogTitleBar("ProspectTogether", OnCloseTitleBar)
                .AddStaticText(ModLang.Get("dialog-show-overlay"), CairoFont.WhiteDetailText(), showOverlayTextBounds)
                .AddSwitch(OnSwitchOverlay, switchBounds, "showOverlaySwitch")
                // Mapmode
                .AddDropDown(new string[] { "0", "1" }, new string[] { ModLang.Get("dialog-map-mode-default"), ModLang.Get("dialog-map-mode-heatmap") }, (int)Config.MapMode, OnMapModeSelected, mapModeBounds)
                // Ore selection
                .AddDropDown(Ores.Select((pair) => pair.Value).ToArray(), Ores.Select((pair) => pair.Key).ToArray(), currentHeatmapOreIndex, OnHeatmapOreSelected, oreBounds)
                .AddStaticText(ModLang.Get("dialog-auto-share"), CairoFont.WhiteDetailText(), autoShareTextBounds)
                .AddSwitch(OnSwitchAutoShare, autoShareSwitchBounds, "autoShareSwitch")
                // Group selection
                .AddDropDown(Groups.Select(p => p.Value).ToArray(), Groups.Select(p => p.Key).ToArray(), currentGroupIndex, OnGroupChanged, shareGroupBounds)
                .AddButton(ModLang.Get("dialog-send-all-now"), OnSendAll, sendAllBounds, CairoFont.WhiteDetailText(), EnumButtonStyle.Small)
                .Compose();

            SingleComposer.GetSwitch("showOverlaySwitch").On = Config.RenderTexturesOnMap;
            SingleComposer.GetSwitch("autoShareSwitch").On = Config.AutoShare;
        }

        private bool OnSendAll()
        {
            Storage.SendAll();
            return true;
        }

        private void OnGroupChanged(string code, bool selected)
        {
            Config.ShareGroupUid = int.Parse(code);
            Config.Save(capi);
            Storage.RequestInfo();
        }

        private void OnCloseTitleBar()
        {
            Config.ShowGui = false;
            Config.Save(capi);
            TryClose();
        }

        private void OnSwitchOverlay(bool value)
        {
            Config.RenderTexturesOnMap = value;
            Config.Save(capi);
            RebuildMap(true);
        }

        private void OnSwitchAutoShare(bool value)
        {
            Config.AutoShare = value;
            Config.Save(capi);
        }

        private void OnMapModeSelected(string code, bool selected)
        {
            Config.MapMode = (MapMode)int.Parse(code);
            Config.Save(capi);
            RebuildMap(true);
        }

        private void OnHeatmapOreSelected(string code, bool selected)
        {
            Config.HeatMapOre = code;
            Config.Save(capi);
            RebuildMap(true);
        }
    }
}
