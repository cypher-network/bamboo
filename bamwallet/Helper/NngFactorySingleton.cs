// CypherNetwork BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0.
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.IO;
using nng;

namespace BAMWallet.Helper;

public sealed class NngFactorySingleton
{
    private static readonly Lazy<NngFactorySingleton> Lazy = new(() => new NngFactorySingleton());
    public static NngFactorySingleton Instance => Lazy.Value;

    public NngFactorySingleton()
    {
        var managedAssemblyPath = Path.GetDirectoryName(GetType().Assembly.Location);
        var alc = new NngLoadContext(managedAssemblyPath);
        Factory = NngLoadContext.Init(alc);
    }

    internal IAPIFactory<INngMsg> Factory { get; }
}