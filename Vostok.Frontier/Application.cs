using Microsoft.AspNetCore.Hosting;
using Serilog;
using Serilog.Events;
using Vstk.Commons.Extensions.UnitConvertions;
using Vstk.Hosting;
using Vstk.Instrumentation.AspNetCore;
using Vstk.Logging.Serilog;
using Vstk.Logging.Serilog.Enrichers;
using Vstk.Metrics;

namespace Vstk.Frontier
{
    public class Application : AspNetCoreVostokApplication
    {
        protected override void OnStarted(IVostokHostingEnvironment hostingEnvironment)
        {
            hostingEnvironment.MetricScope.SystemMetrics(1.Minutes());
        }

        protected override IWebHost BuildWebHost(IVostokHostingEnvironment hostingEnvironment)
        {
            var loggerConfiguration = new LoggerConfiguration()
                .Enrich.With<ThreadEnricher>()
                .Enrich.With<FlowContextEnricher>()
                .MinimumLevel.Debug()
                .WriteTo.Airlock(LogEventLevel.Information);
            if (hostingEnvironment.Log != null)
                loggerConfiguration = loggerConfiguration.WriteTo.VostokLog(hostingEnvironment.Log);
            var logger = loggerConfiguration.CreateLogger();
            return new WebHostBuilder()
                .UseKestrel()
                .UseUrls($"http://*:{hostingEnvironment.Configuration["port"]}/")
                .AddVostokServices()
                //.ConfigureServices(s => s.AddMvc())
                .UseSerilog(logger)
                //.Configure(app =>
                //{
                //    var env = app.ApplicationServices.GetRequiredService<IHostingEnvironment>();
                //    app.UseVostok();
                //    if (env.IsDevelopment())
                //        app.UseDeveloperExceptionPage();
                //    app.UseMvc();
                //})
                .UseStartup<Startup>()
                .Build();
        }
    }
}