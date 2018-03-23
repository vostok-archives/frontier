using Newtonsoft.Json;

namespace Vstk.Frontier.Dto
{
    public class PkpReport : Report
    {
        [JsonProperty("date-time")]
        public string DateTime { get; set; }

        [JsonProperty("hostname")]
        public string Hostname { get; set; }

        [JsonProperty("port")]
        public int Port { get; set; }

        [JsonProperty("effective-expiration-date")]
        public string EffectiveExpirationDate { get; set; }

        [JsonProperty("include-subdomains")]
        public bool IncludeSubdomains { get; set; }

        [JsonProperty("noted-hostname")]
        public string NotedHostname { get; set; }

        [JsonProperty("served-certificate-chain")]
        public string[] ServedCertificateChain { get; set; }

        [JsonProperty("validated-certificate-chain")]
        public string[] ValidatedCertificateChain { get; set; }

        [JsonProperty("known-pins")]
        public string[] KnownPins { get; set; }

        public override string GetProject() => GetServiceFromHostName(Hostname);

    }
}