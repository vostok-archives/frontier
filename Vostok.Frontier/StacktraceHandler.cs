using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SourcemapToolkit.SourcemapParser;
using Vostok.Commons.Extensions.UnitConvertions;
using Vostok.Frontier.Dto;
using Vostok.Logging;
using Vostok.Metrics;

namespace Vostok.Frontier
{
    public class StacktraceHandler : ReportHandler<StacktraceReport>
    {
        private readonly ILog log;
        private readonly FrontierSetings setings;
        private readonly HttpClient httpClient;
        private readonly Regex mappingUrlRegex = new Regex(@"sourceMappingURL=(\S+)\s+$", RegexOptions.Compiled);
        private readonly SourceMapParser sourceMapParser = new SourceMapParser();
        private readonly NullSafeMemoryCache cache;
        private readonly TimeSpan expirationTimeSpan = 24.Hours();

        public StacktraceHandler(string name, IMetricScope metricScope, ILog log, FrontierSetings setings)
            : base(name, metricScope, log)
        {
            this.log = log;
            this.setings = setings;
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = false
            };
            httpClient = new HttpClient(handler);
            cache = new NullSafeMemoryCache();
        }

        protected override async Task HandleReport(StacktraceReport report)
        {
            foreach (var stackFrame in report.Stack.Where(x => !string.IsNullOrEmpty(x.FileName)))
            {
                SourceMap sourceMap = null;
                var url = stackFrame.FileName;
                try
                {
                    var uri = new Uri(url);
                    if (!uri.Scheme.Equals("http", StringComparison.InvariantCultureIgnoreCase) && !uri.Scheme.Equals("https", StringComparison.InvariantCultureIgnoreCase))
                        continue;
                    if (!setings.IsAllowedDomain(uri.Host))
                    {
                        log.Error($"{url} doesn't contain allowed domain");
                        continue;
                    }
                    if (!setings.IsAllowedDomainForSourceMap(uri.Host))
                    {
                        log.Debug($"{url} domain doesn't support sourcemaps");
                        continue;
                    }

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
                    log.Debug("report before patching:\n" + report.ToPrettyJson());
                }
            }
        }

        private async Task<SourceMap> GetMapFromUrl(string url)
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
    }
}