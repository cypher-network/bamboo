// Bamboo (c) by Tangram
//
// Bamboo is licensed under a
// Creative Commons Attribution-NonCommercial-NoDerivatives 4.0 International License.
//
// You should have received a copy of the license along with this
// work. If not, see <http://creativecommons.org/licenses/by-nc-nd/4.0/>.

using System;
using System.Collections.Generic;
using System.Net;

namespace Cli.Configuration
{
    public class IPService
    {
        public IPService(string name, Uri uri)
        {
            _name = name;
            _uri = uri;
        }

        private readonly string _name;
        private readonly Uri _uri;

        public override string ToString()
        {
            return $"{_name} ({_uri})";
        }

        public IPAddress Read()
        {
            using var client = new WebClient();
            var response = client.DownloadString(_uri);
            return IPAddress.Parse(response);
        }
    }

    public class IPServices
    {
        public static IList<IPService> Services { get; } = new List<IPService>()
        {
            new("ident.me", new Uri("https://v4.ident.me")),
            new("ipify.org", new Uri("https://api.ipify.org")),
            new("my-ip.io", new Uri("https://api4.my-ip.io/ip.txt")),
            new("seeip.org", new Uri("https://ip4.seeip.org"))
        };
    }
}