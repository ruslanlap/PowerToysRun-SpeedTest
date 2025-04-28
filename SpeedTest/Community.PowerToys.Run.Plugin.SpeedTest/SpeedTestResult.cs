using System;
using System.Text.Json.Serialization;
using System.ComponentModel;

namespace Community.PowerToys.Run.Plugin.SpeedTest
{
    public class SpeedTestResult : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        [JsonPropertyName("download")]
        public DownloadInfo Download { get; set; }

        [JsonPropertyName("upload")]
        public UploadInfo Upload { get; set; }

        [JsonPropertyName("ping")]
        public PingInfo Ping { get; set; }

        [JsonPropertyName("server")]
        public ServerInfo Server { get; set; }

        [JsonPropertyName("result")]
        public ResultInfo Result { get; set; }

        [JsonPropertyName("timestamp")]
        public DateTime? Timestamp { get; set; }

        private double _cliDownloadMbps;
        [JsonIgnore]
        public double CliDownloadMbps 
        { 
            get => _cliDownloadMbps;
            set
            {
                if (_cliDownloadMbps != value)
                {
                    _cliDownloadMbps = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CliDownloadMbps)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayDownloadSpeed)));
                }
            }
        }

        private double _cliUploadMbps;
        [JsonIgnore]
        public double CliUploadMbps 
        { 
            get => _cliUploadMbps;
            set
            {
                if (_cliUploadMbps != value)
                {
                    _cliUploadMbps = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CliUploadMbps)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayUploadSpeed)));
                }
            }
        }

        private bool _usingCliValues;
        [JsonIgnore]
        public bool UsingCliValues 
        { 
            get => _usingCliValues;
            set
            {
                if (_usingCliValues != value)
                {
                    _usingCliValues = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(UsingCliValues)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayDownloadSpeed)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayUploadSpeed)));
                }
            }
        }

        // Властивості для відображення в інтерфейсі
        [JsonIgnore]
        public double DisplayDownloadSpeed => UsingCliValues ? CliDownloadMbps : Download?.MbpsSpeed ?? 0;

        [JsonIgnore]
        public double DisplayUploadSpeed => UsingCliValues ? CliUploadMbps : Upload?.MbpsSpeed ?? 0;

        public class DownloadInfo : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            private long _bandwidth;
            [JsonPropertyName("bandwidth")]
            public long Bandwidth 
            { 
                get => _bandwidth;
                set
                {
                    if (_bandwidth != value)
                    {
                        _bandwidth = value;
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Bandwidth)));
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MbpsSpeed)));
                    }
                }
            }

            [JsonPropertyName("bytes")]
            public long Bytes { get; set; }

            [JsonPropertyName("elapsed")]
            public int Elapsed { get; set; }

            // Повернення швидкості у Mbps
            [JsonIgnore]
            public double MbpsSpeed => Bandwidth * 8.0 / 1_000_000.0;
        }

        public class UploadInfo : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            private long _bandwidth;
            [JsonPropertyName("bandwidth")]
            public long Bandwidth 
            { 
                get => _bandwidth;
                set
                {
                    if (_bandwidth != value)
                    {
                        _bandwidth = value;
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Bandwidth)));
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(MbpsSpeed)));
                    }
                }
            }

            [JsonPropertyName("bytes")]
            public long Bytes { get; set; }

            [JsonPropertyName("elapsed")]
            public int Elapsed { get; set; }

            // Повернення швидкості у Mbps
            [JsonIgnore]
            public double MbpsSpeed => Bandwidth * 8.0 / 1_000_000.0;
        }

        public class PingInfo
        {
            [JsonPropertyName("latency")]
            public double Latency { get; set; }
        }

        public class ServerInfo
        {
            [JsonPropertyName("name")]
            public string Name { get; set; }

            [JsonPropertyName("location")]
            public string Location { get; set; }

            [JsonPropertyName("country")]
            public string Country { get; set; }

            [JsonPropertyName("id")]
            public int Id { get; set; }

            [JsonPropertyName("host")]
            public string Host { get; set; }
        }

        public class ResultInfo
        {
            [JsonPropertyName("url")]
            public string Url { get; set; }

            [JsonPropertyName("id")]
            public string Id { get; set; }
        }

        public override string ToString()
        {
            // Використовуємо прямі значення з CLI, якщо вони доступні
            double downloadSpeed = UsingCliValues ? CliDownloadMbps : Download?.MbpsSpeed ?? 0;
            double uploadSpeed = UsingCliValues ? CliUploadMbps : Upload?.MbpsSpeed ?? 0;

            return $"Download: {downloadSpeed:F2} Mbps\n" +
                   $"Upload: {uploadSpeed:F2} Mbps\n" +
                   $"Ping: {Ping?.Latency:F2} ms\n" +
                   $"Server: {Server?.Name}, {Server?.Location}\n" +
                   $"URL: {Result?.Url}";
        }

        // Метод Debug для діагностики проблем з JSON
        public string GetDebugInfo()
        {
            return $"Raw bandwidth values:\n" +
                   $"Download bandwidth: {Download?.Bandwidth}\n" +
                   $"Upload bandwidth: {Upload?.Bandwidth}\n" +
                   $"Calculated Mbps:\n" +
                   $"Download: {Download?.MbpsSpeed:F2} Mbps\n" +
                   $"Upload: {Upload?.MbpsSpeed:F2} Mbps\n" +
                   $"CLI values directly:\n" +
                   $"Download: {CliDownloadMbps:F2} Mbps\n" +
                   $"Upload: {CliUploadMbps:F2} Mbps\n" +
                   $"Using CLI values: {UsingCliValues}\n" +
                   $"Display values (what UI will show):\n" +
                   $"Display Download: {DisplayDownloadSpeed:F2} Mbps\n" +
                   $"Display Upload: {DisplayUploadSpeed:F2} Mbps";
        }
    }
}