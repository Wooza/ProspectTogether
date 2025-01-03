using ProtoBuf;
using System.Collections.Generic;

namespace ProspectTogether.Shared
{

    /// <summary>
    /// Client -> Server. Player shares his prospecting data for a certain group.
    /// </summary>
    [ProtoContract(ImplicitFields = ImplicitFields.None)]
    public class PlayerSharesProspectingPacket
    {

        [ProtoMember(1)]
        public List<ProspectInfo> Data = new();

        [ProtoMember(2)]
        public int GroupId = Constants.ALL_GROUP_ID;

        public PlayerSharesProspectingPacket() { }

        public PlayerSharesProspectingPacket(List<ProspectInfo> data, int groupId)
        {
            Data = data;
            GroupId = groupId;
        }
    }

    /// <summary>
    /// Server -> Client. Server broadcasts prospecting data within a group.
    /// </summary>
    [ProtoContract(ImplicitFields = ImplicitFields.None)]
    public class ServerBroadcastsProspectingPacket
    {

        [ProtoMember(1)]
        public List<ProspectInfo> Data = new();

        public ServerBroadcastsProspectingPacket() { }

        public ServerBroadcastsProspectingPacket(List<ProspectInfo> data)
        {
            Data = data;
        }
    }

    /// <summary>
    /// Client -> Server. The client sends this to the server to ask for all prospecting data of a certain group.
    /// </summary>
    [ProtoContract(ImplicitFields = ImplicitFields.None)]
    public class PlayerRequestsInfoForGroupPacket
    {
        [ProtoMember(1)]
        public int GroupId = Constants.ALL_GROUP_ID;

        public PlayerRequestsInfoForGroupPacket() { }

        public PlayerRequestsInfoForGroupPacket(int groupId)
        {
            GroupId = groupId;
        }
    }
}
