using Foundation.Extensions;
using Newtonsoft.Json;
using ProspectTogether.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Util;

namespace ProspectTogether.Client
{
    public class ClientStorage : CommonStorage<ClientModConfig, ICoreClientAPI>
    {
        private IClientNetworkChannel ClientChannel;

        public IEnumerable<KeyValuePair<string, string>> FoundOres { get { return AllOres.Where((pair) => FoundOreNames.Contains(pair.Value)); } }
        private readonly Dictionary<string, string> AllOres = new OreNames();
        private readonly HashSet<string> FoundOreNames = new();

        public event Action<ICollection<ProspectInfo>> OnChanged;

        public IDictionary<ChunkCoordinate, ProspectInfo> Data = new Dictionary<ChunkCoordinate, ProspectInfo>();

        public ClientStorage(ICoreClientAPI api, ClientModConfig config, string fileName) : base(api, config, fileName)
        {
        }

        public virtual void StartClientSide()
        {
            LoadProspectingDataFile();
            Api.Event.LeaveWorld += SaveProspectingDataFile;

            /* Registering this channel when the mod is not installed on the server generates a warning.
             * But this seems to be the only way to test, if the mod is installed on the server.
             */
            ClientChannel = Api.Network.RegisterChannel(ChannelName)
                .RegisterMessageType<PlayerSharesProspectingPacket>()
                .RegisterMessageType<ServerBroadcastsProspectingPacket>()
                .RegisterMessageType<PlayerRequestsInfoForGroupPacket>()
                .SetMessageHandler<ServerBroadcastsProspectingPacket>(OnServerBroadcastsProspecting);
            ConfigureSaveListener();

            Api.Event.PlayerJoin += p =>
            {
                if (Api.World.Player == p)
                {
                    RequestInfo();
                }
            };

        }

        public bool IsModRunningOnServer()
        {
            return ClientChannel != null && ClientChannel.Connected;
        }

        public void RequestInfo()
        {
            if (!IsModRunningOnServer())
            {
                return;
            }

            if (Config.AutoShare)
            {
                ClientChannel.SendPacket(new PlayerRequestsInfoForGroupPacket(Constants.ALL_GROUP_ID));
                ClientChannel.SendPacket(new PlayerRequestsInfoForGroupPacket(Config.ShareGroupUid));
            }
        }

        public void PlayerProspected(List<ProspectInfo> infos)
        {
            List<ProspectInfo> delta = new();

            lock (Lock)
            {
                foreach (ProspectInfo info in infos)
                {
                    // Vanilla mechanism always shares all data, so we compute the delta to what we already have.
                    if (Data.ContainsKey(info.Chunk))
                    {
                        // If we already have a value for the given chunk but the new reading is differnt we still need to update.
                        var existing = Data.Get(info.Chunk);
                        if (existing.Equals(info))
                        {
                            continue;
                        }
                    }

                    delta.Add(info);
                    Data[info.Chunk] = info;
                    foreach (OreOccurence ore in info.Values)
                    {
                        FoundOreNames.Add(ore.Name);
                    }
                }

                if (delta.Count > 0)
                {
                    HasChangedSinceLastSave = true;
                    OnChanged?.Invoke(delta);
                }
            }

            if (!IsModRunningOnServer())
            {
                return;
            }

            if (Config.AutoShare)
            {
                // It's our prospecting data and we want to share it.
                ClientChannel.SendPacket(new PlayerSharesProspectingPacket(delta, Config.ShareGroupUid));
            }
        }

        protected void OnServerBroadcastsProspecting(ServerBroadcastsProspectingPacket packet)
        {
            if (!Config.AutoShare)
            {
                return;
            }

            lock (Lock)
            {
                foreach (ProspectInfo info in packet.Data)
                {
                    Data[info.Chunk] = info;
                    foreach (OreOccurence ore in info.Values)
                    {
                        FoundOreNames.Add(ore.Name);
                    }
                }
                HasChangedSinceLastSave = true;
                OnChanged?.Invoke(packet.Data);
            }
        }

        public void SendAll()
        {
            if (!IsModRunningOnServer())
            {
                Api.ShowChatMessage("The mod is not installed on the server, thus you cannot share your data.");
                return;
            }

            lock (Lock)
            {
                ClientChannel.SendPacket(new PlayerSharesProspectingPacket(Data.Values.ToList(), Config.ShareGroupUid));
            }
        }

        protected override void SaveProspectingDataFile()
        {
            lock (Lock)
            {
                if (HasChangedSinceLastSave)
                {
                    Api.SaveDataFile(FileName, new ClientStoredData(Data.Values.ToList()));
                    HasChangedSinceLastSave = false;
                }
            }
        }

        protected override void LoadProspectingDataFile()
        {
            lock (Lock)
            {
                ClientStoredData loaded = Api.LoadOrCreateDataFile<ClientStoredData>(FileName);
                Data = loaded.ProspectInfos.ToDictionary(item => item.Chunk, item => item);
                foreach (ProspectInfo info in Data.Values)
                {
                    foreach (OreOccurence occurence in info.Values)
                    {
                        FoundOreNames.Add(occurence.Name);
                    }
                }
                HasChangedSinceLastSave = false;
            }
        }
    }

    public class ClientStoredData
    {

        public int Version = 1;

        public List<ProspectInfo> ProspectInfos = new();

        public ClientStoredData()
        {
        }

        [JsonConstructor]
        public ClientStoredData(int version, List<ProspectInfo> prospectInfos)
        {
            Version = version;
            ProspectInfos = prospectInfos;
        }

        public ClientStoredData(List<ProspectInfo> prospectInfos)
        {
            ProspectInfos = prospectInfos;
        }

    }
}
