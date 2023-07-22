using ProtoBuf;
using System.Collections.Generic;

namespace ProspectTogether.Shared
{

    /// <summary>
    /// S -> C. Sent when the player did some prospecting.
    /// </summary>
    [ProtoContract(ImplicitFields = ImplicitFields.None)]
    public class PlayerProspectedPacket
    {

        [ProtoMember(1)]
        public ProspectInfo Data;

        public PlayerProspectedPacket() { }

        public PlayerProspectedPacket(ProspectInfo data)
        {
            Data = data;
        }
    }

    /// <summary>
    /// C -> S. Player shares his prospecting data for a certain group.
    /// </summary>
    [ProtoContract(ImplicitFields = ImplicitFields.None)]
    public class PlayerSharesProspectingPacket
    {

        [ProtoMember(1)]
        public List<ProspectInfo> Data = new List<ProspectInfo>();

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
    /// S -> C. Server broadcasts prospecting data within a group.
    /// </summary>
    [ProtoContract(ImplicitFields = ImplicitFields.None)]
    public class ServerBroadcastsProspectingPacket
    {

        [ProtoMember(1)]
        public List<ProspectInfo> Data = new List<ProspectInfo>();

        public ServerBroadcastsProspectingPacket() { }

        public ServerBroadcastsProspectingPacket(List<ProspectInfo> data)
        {
            Data = data;
        }
    }

    /// <summary>
    /// C -> S. The client sends this to the server to ask for all prospecting data of a certain group.
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
