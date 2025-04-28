using System;
using System.Text.Json.Serialization;

namespace Community.PowerToys.Run.Plugin.SpeedTest
{
    public class SpeedTestResult
    {
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

        public class DownloadInfo
        {
            [JsonPropertyName("bandwidth")]
            public long Bandwidth { get; set; }

            [JsonPropertyName("bytes")]
            public long Bytes { get; set; }

            [JsonPropertyName("elapsed")]
            public int Elapsed { get; set; }

            // Returns download speed in Mbps
            [JsonIgnore]
            public double MbpsSpeed => Bandwidth * 8.0 / 1000000.0;
        }

        public class UploadInfo
        {
            [JsonPropertyName("bandwidth")]
            public long Bandwidth { get; set; }

            [JsonPropertyName("bytes")]
            public long Bytes { get; set; }

            [JsonPropertyName("elapsed")]
            public int Elapsed { get; set; }

            // Returns upload speed in Mbps
            [JsonIgnore]
            public double MbpsSpeed => Bandwidth * 8.0 / 1000000.0;
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
        }

        public class ResultInfo
        {
            [JsonPropertyName("url")]
            public string Url { get; set; }
        }

        public override string ToString()
        {
            return $"Download: {Download?.MbpsSpeed:F2} Mbps\n" +
                   $"Upload: {Upload?.MbpsSpeed:F2} Mbps\n" +
                   $"Ping: {Ping?.Latency:F2} ms\n" +
                   $"Server: {Server?.Name}, {Server?.Location}\n" +
                   $"URL: {Result?.Url}";
        }
    }
}