using ProspectTogether.Shared;

namespace ProspectTogether.Server
{
    public class ServerModConfig : CommonConfig
    {
        public override string ModCode => "ProspectTogetherServer";

        public bool SharingAllowed { get; set; } = true;

    }
}
