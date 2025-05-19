// File: SpeedTestResult.cs
using System;
using System.Text.Json.Serialization;
using System.ComponentModel;
using System.Text; // Required for StringBuilder
using System.Globalization; // Added for CultureInfo

namespace Community.PowerToys.Run.Plugin.SpeedTest
{
    public class SpeedTestResult : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private DownloadInfo _download;
        [JsonPropertyName("download")]
        public DownloadInfo Download
        {
            get => _download;
            set { _download = value; OnPropertyChanged(nameof(Download)); OnPropertyChanged(nameof(DisplayDownloadSpeed)); }
        }

        private UploadInfo _upload;
        [JsonPropertyName("upload")]
        public UploadInfo Upload
        {
            get => _upload;
            set { _upload = value; OnPropertyChanged(nameof(Upload)); OnPropertyChanged(nameof(DisplayUploadSpeed)); }
        }

        private PingInfo _ping;
        [JsonPropertyName("ping")]
        public PingInfo Ping
        {
            get => _ping;
            set { _ping = value; OnPropertyChanged(nameof(Ping)); }
        }

        private ServerInfo _server;
        [JsonPropertyName("server")]
        public ServerInfo Server
        {
            get => _server;
            set { _server = value; OnPropertyChanged(nameof(Server)); }
        }

        private ResultInfo _result;
        [JsonPropertyName("result")]
        public ResultInfo Result
        {
            get => _result;
            set { _result = value; OnPropertyChanged(nameof(Result)); }
        }

        private DateTime? _timestamp;
        [JsonPropertyName("timestamp")]
        public DateTime? Timestamp
        {
            get => _timestamp;
            set { _timestamp = value; OnPropertyChanged(nameof(Timestamp)); }
        }

        private string _isp;
        [JsonPropertyName("isp")]
        public string Isp
        {
            get => _isp;
            set { _isp = value; OnPropertyChanged(nameof(Isp)); }
        }

        private InterfaceInfo _interfaceInfo;
        [JsonPropertyName("interface")]
        public InterfaceInfo Interface // Property name matches "interface" in JSON
        {
            get => _interfaceInfo;
            set { _interfaceInfo = value; OnPropertyChanged(nameof(Interface)); }
        }

        private double _packetLoss;
        [JsonPropertyName("packetLoss")] // Assuming "packetLoss" is a top-level field or part of "ping" in actual JSON
        public double PacketLoss
        {
            get => _packetLoss;
            set { _packetLoss = value; OnPropertyChanged(nameof(PacketLoss)); }
        }


        // These properties might be deprecated if all data comes from JSON directly
        // For now, they will be populated from the main Download/Upload.Bandwidth
        [JsonIgnore]
        public double CliDownloadMbps => Download?.MbpsSpeed ?? 0;

        [JsonIgnore]
        public double CliUploadMbps => Upload?.MbpsSpeed ?? 0;

        [JsonIgnore]
        public bool UsingCliValues { get; set; } = true; // Assume direct JSON parsing is the "CLI value"

        [JsonIgnore]
        public double DisplayDownloadSpeed => Download?.MbpsSpeed ?? 0;

        [JsonIgnore]
        public double DisplayUploadSpeed => Upload?.MbpsSpeed ?? 0;

        [JsonIgnore]
        public string ConnectionType => Interface?.IsVpn == true ? "VPN" : "Direct";


        public class DownloadInfo : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            private long _bandwidth;
            [JsonPropertyName("bandwidth")] // In bytes per second from JSON
            public long Bandwidth
            {
                get => _bandwidth;
                set
                {
                    if (_bandwidth != value)
                    {
                        _bandwidth = value;
                        OnPropertyChanged(nameof(Bandwidth));
                        OnPropertyChanged(nameof(MbpsSpeed));
                    }
                }
            }

            private long _bytes;
            [JsonPropertyName("bytes")]
            public long Bytes
            {
                get => _bytes;
                set { _bytes = value; OnPropertyChanged(nameof(Bytes)); }
            }

            private int _elapsed;
            [JsonPropertyName("elapsed")] // In milliseconds from JSON
            public int Elapsed
            {
                get => _elapsed;
                set { _elapsed = value; OnPropertyChanged(nameof(Elapsed)); }
            }

            [JsonIgnore]
            public double MbpsSpeed => Bandwidth * 8.0 / 1_000_000.0; // Convert Bps to Mbps
        }

        public class UploadInfo : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            private long _bandwidth;
            [JsonPropertyName("bandwidth")] // In bytes per second from JSON
            public long Bandwidth
            {
                get => _bandwidth;
                set
                {
                    if (_bandwidth != value)
                    {
                        _bandwidth = value;
                        OnPropertyChanged(nameof(Bandwidth));
                        OnPropertyChanged(nameof(MbpsSpeed));
                    }
                }
            }

            private long _bytes;
            [JsonPropertyName("bytes")]
            public long Bytes
            {
                get => _bytes;
                set { _bytes = value; OnPropertyChanged(nameof(Bytes)); }
            }

            private int _elapsed;
            [JsonPropertyName("elapsed")] // In milliseconds from JSON
            public int Elapsed
            {
                get => _elapsed;
                set { _elapsed = value; OnPropertyChanged(nameof(Elapsed)); }
            }

            [JsonIgnore]
            public double MbpsSpeed => Bandwidth * 8.0 / 1_000_000.0; // Convert Bps to Mbps
        }

        public class PingInfo : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            private double _latency;
            [JsonPropertyName("latency")] // In milliseconds from JSON
            public double Latency
            {
                get => _latency;
                set { _latency = value; OnPropertyChanged(nameof(Latency)); }
            }

            private double _jitter;
            [JsonPropertyName("jitter")] // In milliseconds from JSON
            public double Jitter
            {
                get => _jitter;
                set { _jitter = value; OnPropertyChanged(nameof(Jitter)); }
            }
        }

        public class ServerInfo : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            private string _name;
            [JsonPropertyName("name")]
            public string Name
            {
                get => _name;
                set { _name = value; OnPropertyChanged(nameof(Name)); }
            }

            private string _location;
            [JsonPropertyName("location")]
            public string Location
            {
                get => _location;
                set { _location = value; OnPropertyChanged(nameof(Location)); }
            }

            private string _country;
            [JsonPropertyName("country")]
            public string Country
            {
                get => _country;
                set { _country = value; OnPropertyChanged(nameof(Country)); }
            }

            private int _id;
            [JsonPropertyName("id")]
            public int Id
            {
                get => _id;
                set { _id = value; OnPropertyChanged(nameof(Id)); }
            }

            private string _host;
            [JsonPropertyName("host")]
            public string Host
            {
                get => _host;
                set { _host = value; OnPropertyChanged(nameof(Host)); }
            }

            private string _ip;
            [JsonPropertyName("ip")]
            public string Ip
            {
                get => _ip;
                set { _ip = value; OnPropertyChanged(nameof(Ip)); }
            }
        }

        public class ResultInfo : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            private string _url;
            [JsonPropertyName("url")]
            public string Url
            {
                get => _url;
                set { _url = value; OnPropertyChanged(nameof(Url)); }
            }

            private string _id;
            [JsonPropertyName("id")]
            public string Id
            {
                get => _id;
                set { _id = value; OnPropertyChanged(nameof(Id)); }
            }
        }

        public class InterfaceInfo : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;
            protected void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

            private string _internalIp;
            [JsonPropertyName("internalIp")]
            public string InternalIp
            {
                get => _internalIp;
                set { _internalIp = value; OnPropertyChanged(nameof(InternalIp)); }
            }

            private string _name;
            [JsonPropertyName("name")]
            public string Name
            {
                get => _name;
                set { _name = value; OnPropertyChanged(nameof(Name)); }
            }

            private string _macAddr;
            [JsonPropertyName("macAddr")]
            public string MacAddr
            {
                get => _macAddr;
                set { _macAddr = value; OnPropertyChanged(nameof(MacAddr)); }
            }

            private bool _isVpn;
            [JsonPropertyName("isVpn")]
            public bool IsVpn
            {
                get => _isVpn;
                set { _isVpn = value; OnPropertyChanged(nameof(IsVpn)); }
            }

            private string _externalIp;
            [JsonPropertyName("externalIp")]
            public string ExternalIp
            {
                get => _externalIp;
                set { _externalIp = value; OnPropertyChanged(nameof(ExternalIp)); }
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            // Форматуємо швидкість завантаження з комою як десятковий розділювач
            sb.AppendLine($"Download: {DisplayDownloadSpeed.ToString("F2", CultureInfo.InvariantCulture).Replace(".", ",")} Mbps");
            sb.AppendLine($"Upload: {DisplayUploadSpeed.ToString("F2", CultureInfo.InvariantCulture).Replace(".", ",")} Mbps");

            if (Ping != null)
            {
                // Використовуємо кому як десятковий розділювач для відповідності бажаному формату
                sb.AppendLine($"Ping: {Ping.Latency.ToString("F2", CultureInfo.InvariantCulture).Replace(".", ",")} ms (Jitter: {Ping.Jitter.ToString("F2", CultureInfo.InvariantCulture).Replace(".", ",")} ms)");
            }

            if (Server != null)
            {
                sb.AppendLine($"Server: {Server.Name} ({Server.Location}, {Server.Country})");
            }

            if (!string.IsNullOrEmpty(Isp))
            {
                sb.AppendLine($"ISP: {Isp}");
            }

            if (Result != null && !string.IsNullOrEmpty(Result.Url))
            {
                sb.AppendLine($"URL: {Result.Url}");
            }

            return sb.ToString();
        }

        public string GetDebugInfo()
        {
            var sb = new StringBuilder();
            sb.AppendLine("SpeedTestResult Debug Info:");
            sb.AppendLine($"Timestamp: {Timestamp}");
            sb.AppendLine($"ISP: {Isp}");
            sb.AppendLine($"Packet Loss: {PacketLoss}");

            sb.AppendLine("\n--- Ping ---");
            sb.AppendLine($"Latency: {Ping?.Latency:F2} ms");
            sb.AppendLine($"Jitter: {Ping?.Jitter:F2} ms");

            sb.AppendLine("\n--- Download ---");
            sb.AppendLine($"Bandwidth (Bytes/sec): {Download?.Bandwidth}");
            sb.AppendLine($"Bytes: {Download?.Bytes}");
            sb.AppendLine($"Elapsed (ms): {Download?.Elapsed}");
            sb.AppendLine($"Mbps: {Download?.MbpsSpeed:F2}");

            sb.AppendLine("\n--- Upload ---");
            sb.AppendLine($"Bandwidth (Bytes/sec): {Upload?.Bandwidth}");
            sb.AppendLine($"Bytes: {Upload?.Bytes}");
            sb.AppendLine($"Elapsed (ms): {Upload?.Elapsed}");
            sb.AppendLine($"Mbps: {Upload?.MbpsSpeed:F2}");

            sb.AppendLine("\n--- Server ---");
            sb.AppendLine($"Name: {Server?.Name}");
            sb.AppendLine($"Location: {Server?.Location}");
            sb.AppendLine($"Country: {Server?.Country}");
            sb.AppendLine($"Host: {Server?.Host}");
            sb.AppendLine($"ID: {Server?.Id}");
            sb.AppendLine($"IP: {Server?.Ip}");

            sb.AppendLine("\n--- Result ---");
            sb.AppendLine($"ID: {Result?.Id}");
            sb.AppendLine($"URL: {Result?.Url}");

            sb.AppendLine("\n--- Interface ---");
            sb.AppendLine($"Internal IP: {Interface?.InternalIp}");
            sb.AppendLine($"Name: {Interface?.Name}");
            sb.AppendLine($"MAC Address: {Interface?.MacAddr}");
            sb.AppendLine($"Is VPN: {Interface?.IsVpn}");
            sb.AppendLine($"External IP: {Interface?.ExternalIp}");

            sb.AppendLine("\n--- Display Values ---");
            sb.AppendLine($"Display Download: {DisplayDownloadSpeed:F2} Mbps");
            sb.AppendLine($"Display Upload: {DisplayUploadSpeed:F2} Mbps");

            return sb.ToString();
        }
    }
}