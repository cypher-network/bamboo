// CypherNetwork BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.IO;
using nng;

namespace BAMWallet.Helper;

public sealed class NngSingletonFactory
{
    private static readonly Lazy<NngSingletonFactory> Lazy = new(() => new NngSingletonFactory());
    public static NngSingletonFactory Instance => Lazy.Value;

    public NngSingletonFactory()
    {
        var managedAssemblyPath = Path.GetDirectoryName(GetType().Assembly.Location);
        var alc = new NngLoadContext(managedAssemblyPath);
        Factory = NngLoadContext.Init(alc);
    }

    internal IAPIFactory<INngMsg> Factory { get; }
}