using Foundation.Extensions;
using Foundation.ModConfig;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using ProspectTogether.Client;
using ProspectTogether.Server;
using ProspectTogether.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace ProspectTogether
{
    public class ProspectTogetherModSystem : ModSystem
    {
        // Old file name from ProspectorInfo
        private const string PROSPECTOR_INFO_FILE_NAME = "vsprospectorinfo.data.json";


        private const string Name = "prospectTogether";

        public const string CLIENT_DATAFILE = "prospectTogetherClient.json";
        public ClientModConfig ClientConfig;
        public ClientStorage ClientStorage;
        public ICoreClientAPI ClientApi;


        public const string SERVER_DATAFILE = "prospectTogetherServer.json";
        public ServerModConfig ServerConfig;
        public ServerStorage ServerStorage;
        public ICoreServerAPI ServerApi;

        public override void StartClientSide(ICoreClientAPI api)
        {
            ClientApi = api;
            ClientConfig = api.LoadOrCreateConfig<ClientModConfig>(this, perServer: true);

            MigrateClientDataFileFromProspectorInfo(api);
            MigrateSerializationFormatToV1(CLIENT_DATAFILE, api);

            ClientStorage = new ClientStorage(api, ClientConfig, CLIENT_DATAFILE);
            ClientStorage.StartClientSide();
            var mapManager = api.ModLoader.GetModSystem<WorldMapManager>();
            // Ingame Prospecting is at 0.75, so we place ourselves just below
            mapManager.RegisterMapLayer<ProspectorOverlayLayer>(Name, 0.76);

            ClientApi.Input.RegisterHotKey(Constants.TOGGLE_GUI_HOTKEY_CODE, ModLang.Get("hotkey-toggle-gui"), GlKeys.P, type: HotkeyType.HelpAndOverlays, ctrlPressed: true);
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            ServerApi = api;
            ServerConfig = api.LoadOrCreateConfig<ServerModConfig>(this);
            MigrateSerializationFormatToV1(SERVER_DATAFILE, api);
            MigrateServerV1ToV2(SERVER_DATAFILE, api);

            ServerStorage = new ServerStorage(api, ServerConfig, SERVER_DATAFILE);
            ServerStorage.StartServerSide();

            api.ChatCommands.Create("pt")
                    .WithDescription("ProspectTogether server main command.")
                    .RequiresPrivilege(Privilege.root)
                    .BeginSubCommand("setsaveintervalminutes")
                        .WithDescription("/pt setsaveintervalminutes [int] - How often should the prospecting data be saved to disk.<br/>" +
                                         "The data is also saved when leaving a world.<br/>" +
                                         "Sets the \"SaveIntervalMinutes\" config option (default = 1)")
                        .WithArgs(api.ChatCommands.Parsers.IntRange("interval", 1, 60))
                        .RequiresPrivilege(Privilege.root)
                        .HandleWith(OnSetSaveIntervalMinutes)
                    .EndSubCommand();
        }

        private TextCommandResult OnSetSaveIntervalMinutes(TextCommandCallingArgs args)
        {
            ServerConfig.SaveIntervalMinutes = (int)args.Parsers[0].GetValue();
            ServerConfig.Save(ServerApi);
            ServerStorage.ConfigureSaveListener();
            return TextCommandResult.Success($"Set Server SaveIntervalMinutes to {ServerConfig.SaveIntervalMinutes}.");
        }

        private static void MigrateClientDataFileFromProspectorInfo(ICoreClientAPI api)
        {
            var oldPath = Path.Combine(GamePaths.DataPath, "ModData", api.GetWorldId(), PROSPECTOR_INFO_FILE_NAME);
            if (!File.Exists(oldPath))
            {
                return;
            }

            var newPath = Path.Combine(GamePaths.DataPath, "ModData", api.GetWorldId(), CLIENT_DATAFILE);
            if (File.Exists(newPath))
            {
                return;
            }
            File.Copy(oldPath, newPath, false);
        }

        private static void MigrateSerializationFormatToV1(string filename, ICoreAPI api)
        {
            // Try to migrate old stored data
            var dataPath = Path.Combine(GamePaths.DataPath, "ModData", api.GetWorldId(), filename);
            if (!File.Exists(dataPath))
            {
                return;
            }
            try
            {
                var content = File.ReadAllText(dataPath);

                // The format used by ProspectorInfo has an array at the top level.
                var result = JToken.Parse(content);
                if (!(result is JArray))
                {
                    return;
                }
                JArray rootArray = result as JArray;

                // Remove entries that could not be parsed in the past.
                List<JObject> toDelete = new();
                foreach (JObject item in rootArray.Cast<JObject>())
                {
                    if (!item.ContainsKey("Values") || item["Values"].Type == JTokenType.Null)
                    {
                        toDelete.Add(item);
                    }
                }
                foreach (JObject item in toDelete)
                {
                    rootArray.Remove(item);
                }

                // Remove old values and group X and Z into chunk.
                foreach (JObject entry in rootArray.Cast<JObject>())
                {
                    JObject chunk = new()
                    {
                    { "X", entry["X"] },
                    { "Z", entry["Z"] }
                };
                    entry.Add("Chunk", chunk);
                    entry.Remove("Message");
                    entry.Remove("X");
                    entry.Remove("Z");
                }

                // Add version header
                JObject newRoot = new();
                newRoot["Version"] = new JValue(1);
                newRoot["ProspectInfos"] = rootArray;

                File.WriteAllText(dataPath, newRoot.ToString(Newtonsoft.Json.Formatting.None));
            }
            catch (Exception e)
            {
                api.World.Logger.Error($"Failed to migrate prospecting data file at '{dataPath}', with an error of '{e}'! Either delete that file or check what is causing the problem.");
                throw;
            }
        }
        private static void MigrateServerV1ToV2(String filename, ICoreAPI api)
        {
            var dataPath = Path.Combine(GamePaths.DataPath, "ModData", api.GetWorldId(), filename);
            if (!File.Exists(dataPath))
            {
                return;
            }

            try
            {
                var content = File.ReadAllText(dataPath);
                var result = JToken.Parse(content);
                var rootNode = result as JObject;
                int version = rootNode.GetValue("Version").ToObject<int>();
                if (version != 1)
                {
                    // Nothing to do for us
                    return;
                }

                var newPath = Path.Combine(GamePaths.DataPath, "ModData", api.GetWorldId(), filename + ".bak_v1_to_v2");
                if (File.Exists(newPath))
                {
                    // Something strange happened
                    throw new Exception("Tried to migrate server data to v2, but backup already exists?");
                }
                File.Copy(dataPath, newPath, false);


                JObject allGroup = new()
                {
                    ["GroupId"] = new JValue(Constants.ALL_GROUP_ID),
                    ["Info"] = rootNode["ProspectInfos"]
                };

                JArray groupArray = new()
                {
                    allGroup
                };

                JObject newRoot = new()
                {
                    ["Version"] = new JValue(2),
                    ["InfoPerGroup"] = groupArray
                };

                File.WriteAllText(dataPath, newRoot.ToString(Newtonsoft.Json.Formatting.None));
            }
            catch (Exception e)
            {
                api.World.Logger.Error($"Failed to migrate prospecting data file from v1 to v2 at '{dataPath}', with an error of '{e}'!");
                throw;
            }
        }

        public override void Start(ICoreAPI api)
        {
            var prospectTogetherPatches = new Harmony("ProspectTogether.patches");
            prospectTogetherPatches.PatchAll(Assembly.GetExecutingAssembly());
        }

    }
}