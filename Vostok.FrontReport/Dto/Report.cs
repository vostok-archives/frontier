using Newtonsoft.Json;

namespace Vostok.FrontReport.Dto
{
    public class Report
    {
        [JsonProperty("@timestamp")]
        public string Timestamp { get; set; }

        [JsonProperty("frontreport-host")]
        public string Host { get; set; }

        [JsonProperty("service")]
        public string Service { get; set; }
    }
}