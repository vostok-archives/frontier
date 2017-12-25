using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Newtonsoft.Json;
using Vostok.Metrics;
using Vostok.Metrics.Meters;

namespace Vostok.FrontReport
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
    }

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
    }

    public interface IReportHandler
    {
        bool CanHandle(string requestPath);
        void Handle(HttpContext context);
    }

    public class ReportHandler<T> : IReportHandler
        where T: Report, new()
    {
        private readonly string name;

        public ReportHandler(string name, IMetricScope metricScope)
        {
            this.name = name;
            metricScope = metricScope.WithTag(MetricsTagNames.Operation, name);
            TotalCounter = metricScope.Counter("total");
            ErrorCounter = metricScope.Counter("errors");
        }

        public bool CanHandle(string requestPath)
        {
            return requestPath.Contains(name);
        }

        public void Handle(HttpContext context)
        {
            TotalCounter.Add();
            T report;
            try
            {
                var streamReader = new StreamReader(context.Request.Body);
                var body = streamReader.ReadToEnd();
                report = body.FromJson<T>();
            }
            catch (Exception e)
            {
                ErrorCounter.Add();
                context.Response.StatusCode = (int) HttpStatusCode.BadRequest;
                return;
            }
            report.Timestamp = DateTime.UtcNow.ToString("O");
            report.Host = context.Request.Host.Value;
        }

        public ICounter TotalCounter { get; set; }
        public ICounter ErrorCounter { get; set; }
    }

    public class HttpHandler
    {
        private readonly FrontReportSetings setings;
        private readonly HashSet<string> domainWhiteList;
        private IReportHandler[] reportHandlers;

        public HttpHandler(FrontReportSetings setings, IMetricScope metricScope)
        {
            this.setings = setings;
            domainWhiteList = setings.DomainWhitelist?.ToHashSet();
            reportHandlers = new[]
            {
                new ReportHandler<>(), 
            };
        }

        public async Task HandleRequest(HttpContext context)
        {
            AddCorsHeaders(context);

            var requestPath = context.Request.Path.Value;
            context.Response.ContentType = "application/json";
            if (string.IsNullOrEmpty(requestPath))
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                return;
            }
            if (requestPath.Contains("csp"))
            {
                //Json
            }
            await context.Response.WriteAsync("FrontReport is running");
        }

        private void AddCorsHeaders(HttpContext context)
        {
            var origin = context.Request.Headers["Origin"];
            if (domainWhiteList != null && domainWhiteList.Count > 0)
            {
                if (!domainWhiteList.Contains(origin))
                    return;
            }
            context.Response.Headers["Access-Control-Allow-Origin"] = origin;
            context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, OPTIONS";
            context.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type";
        }
    }
}