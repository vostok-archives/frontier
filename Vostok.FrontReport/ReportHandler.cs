using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Vostok.FrontReport.Dto;
using Vostok.Logging;
using Vostok.Metrics;
using Vostok.Metrics.Meters;

namespace Vostok.FrontReport
{
    public interface IReportHandler
    {
        bool CanHandle(string requestPath);
        Task Handle(HttpContext context);
    }

    public class ReportHandler<T> : IReportHandler
        where T : Report, new()
    {
        private readonly string name;
        private readonly ILog log;
        private readonly ICounter totalCounter;
        private readonly ICounter errorCounter;

        public ReportHandler(string name, IMetricScope metricScope, ILog log)
        {
            this.name = name;
            this.log = log;
            metricScope = metricScope.WithTag(MetricsTagNames.Operation, name);
            totalCounter = metricScope.Counter("total");
            errorCounter = metricScope.Counter("errors");
        }

        public bool CanHandle(string requestPath)
        {
            return requestPath.Contains(name);
        }

        public async Task Handle(HttpContext context)
        {
            totalCounter.Add();
            T report;
            try
            {
                var streamReader = new StreamReader(context.Request.Body);
                var body = await streamReader.ReadToEndAsync();
                report = body.FromJson<T>();
                await HandleReport(report);
            }
            catch (Exception e)
            {
                errorCounter.Add();
                context.Response.StatusCode = (int) HttpStatusCode.BadRequest;
                log.Error(e);
                return;
            }
            report.Timestamp = DateTime.UtcNow.ToString("O");
            report.Host = context.Request.Host.Value;
            log.Debug("report:\n" + report.ToPrettyJson());
        }

        protected virtual Task HandleReport(T report)
        {
            return Task.CompletedTask;
        }
    }
}