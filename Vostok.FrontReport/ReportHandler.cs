using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SourcemapToolkit.SourcemapParser;
using Vostok.Commons.Extensions.UnitConvertions;
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
        }

        protected virtual Task HandleReport(T report)
        {
            return Task.CompletedTask;
        }
    }

    public class StacktraceHandler : ReportHandler<StacktraceReport>
    {
        private readonly ILog log;
        private readonly HttpClient httpClient;
        private readonly Regex mappingUrlRegex = new Regex(@"sourceMappingURL=(\S+)\s+$", RegexOptions.Compiled);
        private readonly SourceMapParser sourceMapParser = new SourceMapParser();
        private readonly NullSafeMemoryCache cache;
        private readonly TimeSpan expirationTimeSpan = 24.Hours();

        public StacktraceHandler(string name, IMetricScope metricScope, ILog log)
            : base(name, metricScope, log)
        {
            this.log = log;
            httpClient = new HttpClient();
            cache = new NullSafeMemoryCache();
        }

        protected override async Task HandleReport(StacktraceReport report)
        {
            foreach (var stackFrame in report.Stack)
            {
                SourceMap sourceMap = null;
                var url = stackFrame.FileName;
                try
                {
                    if (!cache.Get(url, out sourceMap))
                    {
                        sourceMap = await GetMapFromUrl(url);
                        cache.Set(url, sourceMap, expirationTimeSpan);
                    }
                }
                catch (Exception e)
                {
                    log.Error($"failed to get sourcemap from url: {url}", e);
                }
                var mappingEntry = sourceMap?.GetMappingEntryForGeneratedSourcePosition(
                    new SourcePosition {ZeroBasedLineNumber = stackFrame.LineNumber - 1, ZeroBasedColumnNumber = stackFrame.ColumnNumber - 1});
                if (mappingEntry != null)
                {
                    stackFrame.LineNumber = mappingEntry.OriginalSourcePosition.ZeroBasedLineNumber + 1;
                    stackFrame.ColumnNumber = mappingEntry.OriginalSourcePosition.ZeroBasedColumnNumber + 1;
                    stackFrame.FileName = mappingEntry.OriginalFileName;
                    stackFrame.FunctionName = mappingEntry.OriginalName;
                }
            }
        }

        private async Task<SourceMap> GetMapFromUrl(string url)
        {
            try
            {
                var jsOriginalSource = await httpClient.GetStringAsync(url);
                var match = mappingUrlRegex.Match(jsOriginalSource);
                if (!match.Success)
                {
                    log.Error($"failed to find sourcemap URL in JS file: {url}");
                    return null;
                }
                var sourceMapRelativeUri = match.Groups[1].Value;
                var sourceMapUrl = new Uri(new Uri(url), sourceMapRelativeUri);
                var sourceMapStream = await httpClient.GetStreamAsync(sourceMapUrl);
                return sourceMapParser.ParseSourceMap(new StreamReader(sourceMapStream));
            }
            catch (Exception e)
            {
                log.Error($"failed to get sourcemap from url: {url}", e);
                return null;
            }
        }
    }
}