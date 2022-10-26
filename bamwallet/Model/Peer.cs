using MessagePack;

namespace BAMWallet.Model;

[MessagePackObject]
public class Peer
{
    [Key(0)] public byte[] IpAddress { get; init; }
    [Key(1)] public byte[] HttpPort { get; init; }
    [Key(2)] public byte[] HttpsPort { get; init; }
    [Key(3)] public ulong BlockCount { get; set; }
    [Key(4)] public ulong ClientId { get; init; }
    [Key(5)] public byte[] TcpPort { get; set; }
    [Key(6)] public byte[] DsPort { get; set; }
    [Key(7)] public byte[] WsPort { get; set; }
    [Key(8)] public byte[] Name { get; set; }
    [Key(9)] public byte[] PublicKey { get; set; }
    [Key(10)] public byte[] Signature { get; set; }
    [Key(11)] public byte[] Version { get; set; }
    [Key(12)] public long Timestamp { get; set; }
}