using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using MessagePack;
using Newtonsoft.Json;
using OutWit.Common.Abstract;
using OutWit.Common.Aspects;
using OutWit.Common.Utils;
using OutWit.Common.Values;

namespace OutWit.Communication.Model
{
    [MessagePackObject]
    [JsonObject]
    public class HostInfo : ModelBase
    {
        #region Constructors

        public HostInfo()
        {
            Host = "";
            Port = null;
            UseSsl = false;
            UseWebSocket = false;
            Path = "";
        }

        #endregion

        #region Functions

        public override string ToString()
        {
            return BuildConnection();
        }

        public HostInfo AppendPath(string path)
        {
            var host = Clone();
            if(string.IsNullOrEmpty(host.Path))
                host.Path = path;
            else if(!string.IsNullOrEmpty(path))
            {
                if (path.StartsWith("/"))
                    host.Path = $"{Path}{path}";
                else
                    host.Path = $"{Path}/{path}";
            }

            return host;
        }

        public virtual string BuildConnection(bool withPath = true)
        {
            string url = UseSsl
                ? (UseWebSocket ? "wss://" : "https://")
                : (UseWebSocket ? "ws://" : "http://");

            url += Host;

            if (Port != null)
                url += $":{Port}";

            if (string.IsNullOrEmpty(Path) || !withPath)
                return $"{url}/";

            if (Path.StartsWith("/"))
                url = $"{url}{Path}";
            else
                url = $"{url}/{Path}";

            if(!url.EndsWith("/"))
                return $"{url}/";

            return url;
        }

        public void ValidateHost()
        {
            if (Host.StartsWith("http://"))
            {
                Host = Host.Replace("http://", "");
                UseWebSocket = false;
                UseSsl = false;
            }

            if (Host.StartsWith("https://"))
            {
                Host = Host.Replace("https://", "");
                UseWebSocket = false;
                UseSsl = true;
            }

            if (Host.StartsWith("ws://"))
            {
                Host = Host.Replace("ws://", "");
                UseWebSocket = true;
                UseSsl = false;
            }

            if (Host.StartsWith("wss://"))
            {
                Host = Host.Replace("wss://", "");
                UseWebSocket = true;
                UseSsl = true;
            }

            if (Host.EndsWith("/"))
                Host = Host.TrimEnd(1);

            var portStart = Host.IndexOf(':');
            var portEnd = portStart > -1
                ? Host.IndexOf('/', portStart)
                : -1;
            string portString = "";
            if (portStart > -1 && portEnd > -1)
                portString = Host.Substring(portStart + 1, portEnd - portStart - 1);
            else if (portStart > -1)
                portString = Host.Substring(portStart + 1, Host.Length - portStart - 1);

            if(!string.IsNullOrEmpty(portString) && int.TryParse(portString, out int port))
                Port = port;

            if (portEnd > -1)
                Path = Host.Substring(portEnd + 1, Host.Length - portEnd - 1);

            if(portStart > -1)
                Host = Host.Substring(0, portStart);
        }

        #endregion

        #region Operators

        public static explicit operator string(HostInfo info)
        {
            return info.Connection;
        }

        public static explicit operator HostInfo(string url)
        {
            var info = new HostInfo { Host = url };
            info.ValidateHost();
            return info;
        }

        #endregion

        #region ModelBase

        public override bool Is(ModelBase modelBase, double tolerance = DEFAULT_TOLERANCE)
        {
            if (modelBase is not HostInfo hostInfo)
                return false;

            return Host.Is(hostInfo.Host) &&
                   Port.Is(hostInfo.Port) &&
                   UseSsl.Is(hostInfo.UseSsl) &&
                   UseWebSocket.Is(hostInfo.UseWebSocket) &&
                   Path.Is(hostInfo.Path);
        }

        public override HostInfo Clone()
        {
            return new HostInfo
            {
                Host = Host,
                Port = Port,
                UseSsl = UseSsl,
                UseWebSocket = UseWebSocket,
                Path = Path
            };
        }

        #endregion

        #region Properties

        [Key(0)]
        [JsonProperty]
        public string Host { get; set; }

        [Key(1)]
        [JsonProperty]
        public int? Port { get; set; }

        [Key(2)]
        [JsonProperty]
        public bool UseSsl { get; set; }

        [Key(3)]
        [JsonProperty]
        public bool UseWebSocket { get; set; }

        [Key(4)]
        [JsonProperty]
        public string Path { get; set; }

        [JsonIgnore]
        [IgnoreMember]
        public string Connection => BuildConnection();

        #endregion
    }
}
