using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vostok.Hosting;
using Vostok.Logging;

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
        [UsedImplicitly]
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<FrontReportSetings>(options => Configuration.GetSection("FrontReport").Bind(options));
            services.AddSingleton(x => x.GetService<IVostokHostingEnvironment>().Log);
            services.AddSingleton<HttpHandler>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        [UsedImplicitly]
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILog log, HttpHandler httpHandler)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            app.UseStaticFiles();
            app.Run(httpHandler.Invoke);
            //app.UseVostok();
            log.Info("Configured app");
        }
    }
}
