using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Vstk.Frontier.Dto;
using Vstk.Logging;
using Vstk.Metrics;
using Vstk.Metrics.Meters;

namespace Vstk.Frontier
{
    public interface IReportHandler : IDisposable
    {
        bool CanHandle(string requestPath);
        Task<Report> Handle(HttpContext context);
        string Name { get; }
    }

    public class ReportHandler<T> : IReportHandler
        where T : Report, new()
    {
        public string Name { get; }
        private readonly ILog log;
        private readonly ICounter totalCounter;
        private readonly ICounter errorCounter;

        public ReportHandler(string name, IMetricScope metricScope, ILog log)
        {
            Name = name;
            this.log = log;
            metricScope = metricScope.WithTag(MetricsTagNames.Operation, name);
            totalCounter = metricScope.Counter("total");
            errorCounter = metricScope.Counter("errors");
        }

        public bool CanHandle(string requestPath)
        {
            return requestPath.Contains(Name);
        }

        public async Task<Report> Handle(HttpContext context)
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
                return null;
            }
            //report.Timestamp = DateTime.UtcNow.ToString("O");
            report.Host = context.Request.Host.Value;
            log.Debug("report:\n" + report.ToPrettyJson());
            return report;
        }

        protected virtual Task HandleReport(T report)
        {
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            (totalCounter as IDisposable)?.Dispose();
            (errorCounter as IDisposable)?.Dispose();
        }
    }
}