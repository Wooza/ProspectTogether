﻿using ProspectTogether.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace ProspectTogether.Client
{
    public class ProspectTogetherSettingsDialog
    {
        private const string ID_SWITCH_AUTOSHARE = "autoShare";
        private const string ID_DROPDOWN_GROUP = "group";
        private const string ID_BUTTON_SENDALL = "sendAll";
        private readonly ClientModConfig Config;

        // Callback to update the map
        private readonly Action<bool> RebuildMap;

        // Ores that are displayed in the ore selection
        private readonly List<KeyValuePair<string, string>> Ores;
        private readonly int NumFixedOreEntries;

        // Groups that are displayed in the group selection
        private readonly List<KeyValuePair<string, string>> Groups;
        private readonly int NumFixedGroupEntries;
        private readonly ClientStorage Storage;

        private readonly ICoreClientAPI Capi;

        public ProspectTogetherSettingsDialog(ICoreClientAPI capi, ClientModConfig config, Action<bool> rebuildMap, ClientStorage storage)
        {
            Capi = capi;
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
        }

        private void UpdateOres()
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
            }
        }

        private void UpdateGroups()
        {
            HashSet<string> oldGroups = Groups.Select((pair) => pair.Value).Skip(NumFixedGroupEntries).ToHashSet();
            HashSet<string> newGroups = Capi.World.Player.Groups.Select((g) => g.GroupUid.ToString()).ToHashSet();

            if (!oldGroups.Equals(newGroups))
            {
                if (Groups.Count > NumFixedGroupEntries)
                {
                    Groups.RemoveRange(NumFixedGroupEntries, Groups.Count - NumFixedGroupEntries);
                }

                foreach (PlayerGroupMembership group in Capi.World.Player.Groups)
                {
                    Groups.Add(new KeyValuePair<string, string>(group.GroupName, group.GroupUid.ToString()));
                }
            }
        }

        public void Compose(string key, GuiDialogWorldMap guiDialogWorldMap, GuiComposer compo)
        {
            UpdateOres();
            UpdateGroups();

            // Auto-sized dialog at the center of the screen
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightMiddle);
            ElementBounds backgroundBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            ElementBounds dialogContainerBounds = ElementBounds.Fixed(0, 40, 200, 170);
            backgroundBounds.BothSizing = ElementSizing.FitToChildren;
            backgroundBounds.WithChildren(dialogContainerBounds);

            double width = 200;
            double x = 20;
            ElementBounds mapModeBounds = ElementBounds.Fixed(x, 45, width, 30);
            ElementBounds oreBounds = ElementBounds.Fixed(x, mapModeBounds.fixedY + 40, width, 30);
            ElementBounds autoShareSwitchBounds = ElementBounds.Fixed((int)x, (int)(oreBounds.fixedY + 40));
            ElementBounds autoShareTextBounds = ElementBounds.Fixed(60, oreBounds.fixedY + 45, width - 60, 40);
            ElementBounds shareGroupBounds = ElementBounds.Fixed(x, autoShareSwitchBounds.fixedY + 40, width, 30);
            ElementBounds sendAllBounds = ElementBounds.Fixed(x, shareGroupBounds.fixedY + 40, width, 30);

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

            var composer = Capi.Gui.CreateCompo("ProspectTogether Settings", dialogBounds)
                 .AddShadedDialogBG(backgroundBounds)
                 .AddDialogTitleBar("ProspectTogether", () => { guiDialogWorldMap.Composers[key].Enabled = false; })
                 // Mapmode
                 .AddDropDown(new string[] { "0", "1" }, new string[] { ModLang.Get("dialog-map-mode-default"), ModLang.Get("dialog-map-mode-heatmap") }, (int)Config.MapMode, OnMapModeSelected, mapModeBounds)
                 // Ore selection
                 .AddDropDown(Ores.Select((pair) => pair.Value).ToArray(), Ores.Select((pair) => pair.Key).ToArray(), currentHeatmapOreIndex, OnHeatmapOreSelected, oreBounds)
                 .AddStaticText(ModLang.Get("dialog-auto-share"), CairoFont.WhiteSmallText(), autoShareTextBounds)
                 .AddSwitch(OnSwitchAutoShare, autoShareSwitchBounds, ID_SWITCH_AUTOSHARE)
                 // Group selection
                 .AddDropDown(Groups.Select(p => p.Value).ToArray(), Groups.Select(p => p.Key).ToArray(), currentGroupIndex, OnGroupChanged, shareGroupBounds, ID_DROPDOWN_GROUP)
                 .AddButton(ModLang.Get("dialog-send-all-now"), OnSendAll, sendAllBounds, CairoFont.WhiteSmallText().WithOrientation(EnumTextOrientation.Center), EnumButtonStyle.Small, ID_BUTTON_SENDALL)
                 .Compose();

            composer.GetSwitch(ID_SWITCH_AUTOSHARE).On = Config.AutoShare;
            composer.Enabled = false;

            // Disable if mod is not running on server
            composer.GetSwitch(ID_SWITCH_AUTOSHARE).Enabled = Storage.IsModRunningOnServer();
            composer.GetDropDown(ID_DROPDOWN_GROUP).Enabled = Storage.IsModRunningOnServer();
            composer.GetButton(ID_BUTTON_SENDALL).Enabled = Storage.IsModRunningOnServer();

            guiDialogWorldMap.Composers[key] = composer;
        }

        private bool OnSendAll()
        {
            Storage.SendAll();
            return true;
        }

        private void OnGroupChanged(string code, bool selected)
        {
            Config.ShareGroupUid = int.Parse(code);
            Config.Save(Capi);
            Storage.RequestInfo();
        }

        private void OnSwitchAutoShare(bool value)
        {
            Config.AutoShare = value;
            Config.Save(Capi);
        }

        private void OnMapModeSelected(string code, bool selected)
        {
            Config.MapMode = (MapMode)int.Parse(code);
            Config.Save(Capi);
            RebuildMap(true);
        }

        private void OnHeatmapOreSelected(string code, bool selected)
        {
            Config.HeatMapOre = code;
            Config.Save(Capi);
            RebuildMap(true);
        }
    }
}
