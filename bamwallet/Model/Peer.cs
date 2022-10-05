namespace BAMWallet.Model;

public class Peer
{
    public string IpAddress { get; init; }
    public string HttpPort { get; init; }
    public string HttpsPort { get; init; }
    public ulong BlockCount { get; set; }
    public ulong ClientId { get; init; }
    public string TcpPort { get; set; }
    public string WsPort { get; set; }
    public string Name { get; set; }
    public string PublicKey { get; set; }
    public string Version { get; set; }
    public long Timestamp { get; set; }
}