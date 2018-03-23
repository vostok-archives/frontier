using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Vostok.Airlock;
using Vostok.Frontier.Dto;
using Vostok.Hosting;
using Vostok.Logging;
using Vostok.Metrics;
using Vostok.Metrics.Meters;

namespace Vostok.Frontier
{
    [UsedImplicitly]
    public class HttpHandler : IDisposable
    {
        private readonly ILog log;
        private readonly IAirlockClient airlockClient;
        private readonly IReportHandler[] reportHandlers;
        private readonly ICounter totalCounter;
        private readonly ICounter errorCounter;
        private readonly string environment;

        public HttpHandler(FrontierSetings setings, IMetricScope metricScope, ILog log, IAirlockClient airlockClient)
        {
            this.log = log;
            this.airlockClient = airlockClient;
            log.Debug("settings: " + setings?.ToPrettyJson());
            var httpScope = metricScope.WithTag(MetricsTagNames.Type, "http");
            reportHandlers = new IReportHandler[]
            {
                new StacktraceHandler("stacktracejs", httpScope, log, setings),
                new ReportHandler<CspReport>("csp", httpScope, log), 
                new ReportHandler<PkpReport>("pkp", httpScope, log)
            };
            var handlerScope = metricScope.WithTag(MetricsTagNames.Operation, "handler");
            totalCounter = handlerScope.Counter("total");
            errorCounter = handlerScope.Counter("errors");
            environment = VostokHostingEnvironment.Current.Environment;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                totalCounter.Add();
                log.Debug($"Request {context.Request.Method} {context.Request.Path}");

                if (HttpMethods.IsGet(context.Request.Method))
                {
                    await context.Response.WriteAsync("Frontier is running");
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

                    var routingKey = RoutingKey.Create(report.GetProject(), environment, "frontier_" + reportHandler.Name, RoutingKey.LogsSuffix);
                    log.Debug("Send data via airlock to " + routingKey);
                    airlockClient.Push(routingKey, logEventData, logEventData.Timestamp);
                    context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                }
                else if (HttpMethods.IsOptions(context.Request.Method))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NoContent;
                }
            }
            catch (Exception e)
            {
                errorCounter.Add();
                log.Error(e);
            }
        }

        public void Dispose()
        {
            (totalCounter as IDisposable)?.Dispose();
            (errorCounter as IDisposable)?.Dispose();
            foreach (var reportHandler in reportHandlers)
            {
                reportHandler.Dispose();
            }
        }
    }
}