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
                .RegisterMessageType<PlayerProspectedPacket>()
                .RegisterMessageType<PlayerSharesProspectingPacket>()
                .RegisterMessageType<ServerBroadcastsProspectingPacket>()
                .RegisterMessageType<PlayerRequestsInfoForGroupPacket>()
                .SetMessageHandler<ServerBroadcastsProspectingPacket>(OnServerBroadcastsProspecting)
                .SetMessageHandler<PlayerProspectedPacket>(OnPlayerProspected);
            ConfigureSaveListener();

            Api.Event.PlayerJoin += p =>
            {
                if (Api.World.Player == p)
                {
                    RequestInfo();
                }
            };

        }

        public void RequestInfo()
        {
            if (Config.AutoShare)
            {
                ClientChannel.SendPacket(new PlayerRequestsInfoForGroupPacket(Constants.ALL_GROUP_ID));
                ClientChannel.SendPacket(new PlayerRequestsInfoForGroupPacket(Config.ShareGroupUid));
            }
        }

        private void OnPlayerProspected(PlayerProspectedPacket packet)
        {
            lock (Lock)
            {
                Data[packet.Data.Chunk] = packet.Data;
                foreach (OreOccurence ore in packet.Data.Values)
                {
                    FoundOreNames.Add(ore.Name);
                }
                HasChangedSinceLastSave = true;
                OnChanged?.Invoke(new List<ProspectInfo>() { packet.Data });
            }
            if (Config.AutoShare)
            {
                // It's our prospecting data and we want to share it.
                ClientChannel.SendPacket(new PlayerSharesProspectingPacket(new List<ProspectInfo>() { packet.Data }, Config.ShareGroupUid));
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
