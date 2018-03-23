using JetBrains.Annotations;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vostok.Hosting;
using Vostok.Logging;

namespace Vostok.Frontier
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
            services.Configure<FrontierSetings>(options => Configuration.GetSection("Frontier").Bind(options));
            services.AddSingleton(x => x.GetService<IVostokHostingEnvironment>().Log);
            services.AddSingleton<HttpHandler>();
            services.AddSingleton(x => x.GetService<IOptions<FrontierSetings>>().Value);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        [UsedImplicitly]
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILog log, HttpHandler httpHandler)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            app.UseMiddleware<CorsMiddleware>();
            app.UseStaticFiles();
            app.Run(httpHandler.Invoke);
            //app.UseVostok();
            log.Info("Configured app");
        }
    }
}
