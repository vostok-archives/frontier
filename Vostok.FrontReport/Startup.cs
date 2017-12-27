using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vostok.Hosting;
using Vostok.Logging;
using Vostok.Metrics;

namespace Vostok.FrontReport
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<FrontReportSetings>(options => Configuration.GetSection("FrontReport").Bind(options));
            services.AddMvc()
                .AddJsonOptions(
                    opt =>
                    {
                        opt.SerializerSettings.Converters.Add(new JsonGuidConverter());
                    });
            services.AddSingleton(x => x.GetService<IVostokHostingEnvironment>().Log);
            //services.AddSingleton(
            //    x =>
            //    {
            //        var rootScope = x.GetService<IMetricScope>();
            //        var metricScope = rootScope.WithTag(MetricsTagNames.Type,"api");
            //        return new MetricContainer
            //        {
            //            SuccessCounter = metricScope.WithTag("status","200").Counter(FlushMetricsInterval, "requests"),
            //            ErrorCounter = metricScope.WithTag("status", "500").Counter(FlushMetricsInterval, "requests")
            //        };
            //    });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILog log, IOptions<FrontReportSetings>  setings, IMetricScope metricScope)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            app.UseStaticFiles();
            //app.UseMiddleware<HttpHandler>();
            app.Run(new HttpHandler(setings, metricScope, log).Invoke);
            //app.UseMvc();
            //app.UseVostok();
            log.Info("Configured app");
        }

        private TimeSpan FlushMetricsInterval
        {
            get
            {
                var flushMetricsInterval = Configuration.GetValue("FlushMetricsInterval", MetricClock.DefaultPeriod);
                return flushMetricsInterval;
            }
        }
    }
}
