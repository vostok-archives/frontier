using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Vostok.Airlock;
using Vostok.FrontReport.Dto;
using Vostok.Hosting;
using Vostok.Logging;
using Vostok.Metrics;
using Vostok.Metrics.Meters;

namespace Vostok.FrontReport
{
    [UsedImplicitly]
    public class HttpHandler : IDisposable
    {
        private readonly ILog log;
        private readonly IAirlockClient airlockClient;
        private readonly HashSet<string> domainWhiteList;
        private readonly IReportHandler[] reportHandlers;
        private readonly ICounter successCounter;
        private readonly ICounter errorCounter;
        private readonly string environment;

        public HttpHandler(IOptions<FrontReportSetings> setings, IMetricScope metricScope, ILog log, IAirlockClient airlockClient)
        {
            this.log = log;
            this.airlockClient = airlockClient;
            domainWhiteList = setings?.Value?.DomainWhitelist?.ToHashSet();
            reportHandlers = new IReportHandler[]
            {
                new ReportHandler<CspReport>("csp", metricScope, log), 
                new ReportHandler<PkpReport>("pkp", metricScope, log), 
                new StacktraceHandler("stacktracejs", metricScope, log)
            };
            successCounter = metricScope.WithTag(MetricsTagNames.Status, "200").Counter("requests");
            errorCounter = metricScope.WithTag(MetricsTagNames.Status, "500").Counter("requests");
            environment = VostokHostingEnvironment.Current.Environment;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                log.Debug($"Request {context.Request.Method} {context.Request.Path}");
                AddCorsHeaders(context);

                if (HttpMethods.IsGet(context.Request.Method))
                {
                    await context.Response.WriteAsync("FrontReport is running");
                }
                else if (HttpMethods.IsPost(context.Request.Method))
                {
                    var requestPath = context.Request.Path.Value;
                    if (string.IsNullOrEmpty(requestPath))
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                        return;
                    }
                    var reportHandler = reportHandlers.FirstOrDefault(x => x.CanHandle(requestPath));
                    if (reportHandler == null)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                        return;
                    }

                    var report = await reportHandler.Handle(context);
                    var logEventData = report.ToLogEventData();

                    var routingKey = RoutingKey.Create(report.GetProject(), environment, "frontreport_" + reportHandler.Name, RoutingKey.LogsSuffix);
                    log.Debug("Send data via airlock to " + routingKey);
                    airlockClient.Push(routingKey, logEventData, logEventData.Timestamp);
                    context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                }
                else if (HttpMethods.IsOptions(context.Request.Method))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                }
                successCounter.Add();
            }
            catch (Exception e)
            {
                errorCounter.Add();
                log.Error(e);
            }
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

        public void Dispose()
        {
            (successCounter as IDisposable)?.Dispose();
            (errorCounter as IDisposable)?.Dispose();
            foreach (var reportHandler in reportHandlers)
            {
                reportHandler.Dispose();
            }
        }
    }
}