using Foundation.Extensions;
using Newtonsoft.Json;
using ProspectTogether.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace ProspectTogether.Server
{
    public class ServerStorage : CommonStorage<ServerModConfig, ICoreServerAPI>
    {

        private IServerNetworkChannel ServerChannel;

        // Group id to to chunk to prospecting data
        public Dictionary<int, Dictionary<ChunkCoordinate, ProspectInfo>> Data = new();

        public ServerStorage(ICoreServerAPI api, ServerModConfig config, string fileName) : base(api, config, fileName)
        {
        }

        public virtual void StartServerSide()
        {
            Api.Event.SaveGameLoaded += LoadProspectingDataFile;
            Api.Event.GameWorldSave += SaveProspectingDataFile;
            ConfigureSaveListener();

            ServerChannel = Api.Network.RegisterChannel(ChannelName)
                .RegisterMessageType<PlayerProspectedPacket>()
                .RegisterMessageType<PlayerSharesProspectingPacket>()
                .RegisterMessageType<ServerBroadcastsProspectingPacket>()
                .RegisterMessageType<PlayerRequestsInfoForGroupPacket>()
                .SetMessageHandler<PlayerSharesProspectingPacket>(PlayerSharedProspectingData)
                .SetMessageHandler<PlayerRequestsInfoForGroupPacket>(PlayerRequestsInfoForGroup);
        }
        public virtual void UserProspected(ProspectInfo newData, IServerPlayer byPlayer)
        {

            var packet = new PlayerProspectedPacket(newData);
            ServerChannel.SendPacket(packet, byPlayer);
        }

        public virtual void PlayerSharedProspectingData(IServerPlayer fromPlayer, PlayerSharesProspectingPacket packet)
        {
            if (!IsValidGroup(packet.GroupId, fromPlayer))
            {
                // Invalid
                return;
            }

            lock (Lock)
            {
                if (!Data.ContainsKey(packet.GroupId))
                {
                    Data[packet.GroupId] = new Dictionary<ChunkCoordinate, ProspectInfo>();
                }

                foreach (ProspectInfo info in packet.Data)
                {
                    Data[packet.GroupId][info.Chunk] = info;
                }
                HasChangedSinceLastSave = true;
            }
            ServerBroadcastsProspectingPacket broadCastPacket = new(packet.Data);

            if (packet.GroupId == Constants.ALL_GROUP_ID)
            {
                ServerChannel.BroadcastPacket(broadCastPacket);
            }
            else
            {
                PlayerGroup group = Api.Groups.PlayerGroupsById.Get(packet.GroupId, null);
                if (group != null)
                {
                    ServerChannel.SendPacket(broadCastPacket, Array.ConvertAll(group.OnlinePlayers.ToArray(), i => (IServerPlayer)i));
                }
            }
        }

        private void PlayerRequestsInfoForGroup(IServerPlayer fromPlayer, PlayerRequestsInfoForGroupPacket packet)
        {
            if (!IsValidGroup(packet.GroupId, fromPlayer))
            {
                return;
            }

            ServerBroadcastsProspectingPacket broadCastPacket;
            lock (Lock)
            {
                if (Data.ContainsKey(packet.GroupId))
                {
                    broadCastPacket = new ServerBroadcastsProspectingPacket(Data[packet.GroupId].Values.ToList());
                }
                else
                {
                    return;
                }
            }
            ServerChannel.SendPacket(broadCastPacket, fromPlayer);
        }

        private bool IsValidGroup(int groupId, IServerPlayer player)
        {
            return groupId == Constants.ALL_GROUP_ID || player.GetGroup(groupId) != null;
        }

        protected override void SaveProspectingDataFile()
        {
            lock (Lock)
            {
                if (HasChangedSinceLastSave)
                {
                    List<GroupData> data = new();
                    foreach (KeyValuePair<int, Dictionary<ChunkCoordinate, ProspectInfo>> item in Data)
                    {
                        data.Add(new GroupData(item.Key, item.Value.Values.ToList()));
                    }

                    Api.SaveDataFile(FileName, new ServerStoredData(data));
                    HasChangedSinceLastSave = false;
                }
            }
        }

        protected override void LoadProspectingDataFile()
        {
            lock (Lock)
            {
                ServerStoredData loaded = Api.LoadOrCreateDataFile<ServerStoredData>(FileName);
                Data = loaded.InfoPerGroup.ToDictionary(item => item.GroupId, item => item.Info.ToDictionary(i => i.Chunk, i => i));
                HasChangedSinceLastSave = false;
            }
        }
    }

    public class ServerStoredData
    {

        public int Version = 2;

        public List<GroupData> InfoPerGroup = new();

        public ServerStoredData()
        {
        }

        [JsonConstructor]
        public ServerStoredData(int version, List<GroupData> infoPerGroup)
        {
            Version = version;
            InfoPerGroup = infoPerGroup;
        }

        public ServerStoredData(List<GroupData> infoPerGroup)
        {
            InfoPerGroup = infoPerGroup;
        }
    }

    public class GroupData
    {
        public int GroupId;

        public List<ProspectInfo> Info = new();

        public GroupData() { }

        [JsonConstructor]
        public GroupData(int groupId, List<ProspectInfo> info)
        {
            GroupId = groupId;
            Info = info;
        }
    }
}
