using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Vostok.Logging;

namespace Vostok.Frontier
{
    [UsedImplicitly]
    public class CorsMiddleware
    {
        private readonly RequestDelegate next;
        private readonly FrontierSetings setings;
        private readonly ILog log;

        public CorsMiddleware(RequestDelegate next, FrontierSetings setings, ILog log)
        {
            this.next = next;
            this.setings = setings;
            this.log = log;
        }

        public Task Invoke(HttpContext context)
        {
            var origin = context.Request.Headers["Origin"];
            if (!setings.IsAllowedDomain(origin))
            {
                log.ForContext("domain", origin).Info("domain not in whitelist");
            }
            else
            {
                context.Response.Headers["Access-Control-Allow-Origin"] = origin;
                context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, OPTIONS";
                context.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type";
            }
            return next(context);
        }
    }
}