namespace BAMWallet.Model;

public class Peer
{
    public string HttpEndPoint { get; set; }
    public ulong BlockCount { get; set; }
    public ulong ClientId { get; set; }
    public string Listening { get; set; }
    public string Advertise { get; set; }
    public string Name { get; set; }
    public string PublicKey { get; set; }
    public string Version { get; set; }
    public long Timestamp { get; set; }
}