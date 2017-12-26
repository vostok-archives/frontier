using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Vostok.FrontReport.Dto;
using Vostok.Logging;
using Vostok.Metrics;

namespace Vostok.FrontReport
{
    public class HttpHandler
    {
        private readonly FrontReportSetings setings;
        private readonly HashSet<string> domainWhiteList;
        private readonly IReportHandler[] reportHandlers;

        public HttpHandler(FrontReportSetings setings, IMetricScope metricScope, ILog log)
        {
            this.setings = setings;
            domainWhiteList = setings.DomainWhitelist?.ToHashSet();
            reportHandlers = new IReportHandler[]
            {
                new ReportHandler<CspReport>("csp", metricScope, log), 
                new ReportHandler<PkpReport>("pkp", metricScope, log), 
                new StacktraceHandler("stacktracejs", metricScope, log)
            };
        }

        public async Task HandleRequest(HttpContext context)
        {
            AddCorsHeaders(context);

            if (HttpMethods.IsGet(context.Request.Method))
            {
                await context.Response.WriteAsync("FrontReport is running");
            }
            else if (HttpMethods.IsPost(context.Request.Method))
            {
                var requestPath = context.Request.Path.Value;
                //context.Response.ContentType = "application/json";
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
                await reportHandler.Handle(context);
                context.Response.StatusCode = (int)HttpStatusCode.NoContent;
            }
            else if (HttpMethods.IsOptions(context.Request.Method))
            {
                context.Response.StatusCode = (int)HttpStatusCode.NoContent;
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
    }
}