using FlatSharp.Attributes;

namespace BAMWallet.Model
{
    [FlatBufferTable]
    public class PoSCommitedSum : object
    {
        [FlatBufferItem(1)]
        public virtual string Balance { get; set; }
        [FlatBufferItem(2)]
        public virtual string Difficulty { get; set; }
        [FlatBufferItem(3)]
        public virtual string Difference { get; set; }
    }
}
