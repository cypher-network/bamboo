using System;

namespace BAMWallet.Model;

public ref struct RingConfidentialTransaction
{
    public byte[] M;
    public int Cols;
    public Span<byte[]> PcmOut;
    public Span<byte[]> Blinds;
    public byte[] Preimage;
    public byte[] Pc;
    public byte[] Ki;
    public byte[] Ss;
    public byte[] Bp;
    public byte[] Offsets;
}