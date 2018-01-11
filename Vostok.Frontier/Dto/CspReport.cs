using System;
using Newtonsoft.Json;
using Vostok.Airlock.Logging;

namespace Vostok.Frontier.Dto
{
    public class CspReportBody
    {
        [JsonProperty("document-uri")]
        public string DocumentURI { get; set; }

        [JsonProperty("referrer")]
        public string Referrer { get; set; }

        [JsonProperty("blocked-uri")]
        public string BlockedURI { get; set; }

        [JsonProperty("violated-directive")]
        public string ViolatedDirective { get; set; }

        [JsonProperty("effective-directive")]
        public string EffectiveDirective { get; set; }

        [JsonProperty("original-policy")]
        public string OriginalPolicy { get; set; }
    }

    public class CspReport : Report
    {
        [JsonProperty("csp-report")]
        public CspReportBody Body { get; set; }
        public override LogEventData ToLogEventData()
        {
            var logEventData = base.ToLogEventData();
            LoadStringPropertiesToDictionary(Body, logEventData.Properties);
            return logEventData;
        }

        public override string GetProject() => Body == null ? null : GetServiceFromHostName(new Uri(Body.DocumentURI).Host);
    }
}