// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Net;
using System.Net.Sockets;
using BAMWallet.Helper;

namespace Cli.Helper;

public class Utils
{
    public static int IsFreePort(int p)
    {
        var port = 0;
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        try
        {
            var localEp = new IPEndPoint(IPAddress.Any, p);
            socket.Bind(localEp);
            localEp = (IPEndPoint)socket.LocalEndPoint;
            port = localEp.Port;
        }
        catch (SocketException ex)
        {
            var m = ex.Message;
        }
        finally
        {
            socket.Close();
        }
        return port;
    }

    public static IPEndPoint TryParseAddress(string uriString)
    {
        IPEndPoint endPoint = null;
        if (Uri.TryCreate(uriString, UriKind.Absolute, out var url) &&
            IPAddress.TryParse(url.Host, out var ip))
        {
            endPoint = new IPEndPoint(ip, url.Port);
        }

        return endPoint;
    }

    public static void SetConsoleTitle(string networkMode)
    {
        Console.Title = $"Bamboo v{BAMWallet.Helper.Util.GetAssemblyVersion()} network mode: [{networkMode}]";
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    public static IPAddress GetIpAddress()
    {
        var host1 = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host1.AddressList)
        {
            if (ip.AddressFamily != AddressFamily.InterNetwork) continue;
            if (!ip.IsPrivate())
            {
                return ip;
            }
        }

        return IPAddress.Loopback;
    }
}