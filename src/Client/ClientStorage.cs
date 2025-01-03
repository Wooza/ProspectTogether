using Foundation.Extensions;
using Newtonsoft.Json;
using ProspectTogether.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;

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

        private bool IsModRunningOnServer()
        {
            // This logs an error when the mod is missing on the server side
            // But checking Api.Network.GetChannelState(ChannelName) returns Connected, even if the Mod is not running on the server ?!
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

        public void PlayerProspected(ProspectInfo info)
        {
            lock (Lock)
            {
                Data[info.Chunk] = info;
                foreach (OreOccurence ore in info.Values)
                {
                    FoundOreNames.Add(ore.Name);
                }
                HasChangedSinceLastSave = true;
                OnChanged?.Invoke(new List<ProspectInfo>() { info });
            }

            if (!IsModRunningOnServer())
            {
                return;
            }

            if (Config.AutoShare)
            {
                // It's our prospecting data and we want to share it.
                ClientChannel.SendPacket(new PlayerSharesProspectingPacket(new List<ProspectInfo>() { info }, Config.ShareGroupUid));
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
