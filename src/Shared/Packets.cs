using ProtoBuf;
using System.Collections.Generic;

namespace ProspectTogether.Shared
{
    [ProtoContract(ImplicitFields = ImplicitFields.None)]
    public class ProspectingPacket
    {

        [ProtoMember(1)]
        public List<ProspectInfo> Data = new List<ProspectInfo>();

        [ProtoMember(2)]
        public bool OriginatesFromProPick;

        public ProspectingPacket() { }

        public ProspectingPacket(List<ProspectInfo> data, bool originatesFromProPick)
        {
            Data = data;
            OriginatesFromProPick = originatesFromProPick;
        }
    }
}
