using ProtoBuf;

namespace BAMWallet.Model
{
    [ProtoContract]
    public class PoSCommitedSum
    {
        [ProtoMember(1)]
        public string Balance { get; set; }
        [ProtoMember(2)]
        public string Difficulty { get; set; }
        [ProtoMember(3)]
        public string Difference { get; set; }
    }
}
