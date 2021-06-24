// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore;

using BAMWallet.HD;
using BAMWallet.Rpc.Formatters;
using BAMWallet.Services;

namespace BAMWallet.Rpc
{
    public class SelfHosted : BackgroundService
    {
        private class Startup
        {
            public Startup(IConfiguration configuration)
            {
                Configuration = configuration;
            }

            public IConfiguration Configuration { get; }

            public void ConfigureServices(IServiceCollection services)
            {
                services.AddMvcCore()
                    .AddApiExplorer();

                services.AddMvcCore(options =>
                {
                    options.InputFormatters.Insert(0, new BinaryInputFormatter());
                });

                services.AddSwaggerGen(options =>
                {
                    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                    {
                        License = new Microsoft.OpenApi.Models.OpenApiLicense
                        {
                            Name = "Attribution-NonCommercial-NoDerivatives 4.0 International",
                            Url = new Uri("https://raw.githubusercontent.com/tangramproject/Cypher/master/LICENSE")
                        },
                        Title = "Bamboo Rest API",
                        Version = Helper.Util.GetAssemblyVersion(),
                        Description = "Bamboo Wallet Service.",
                        TermsOfService = new Uri("https://tangrams.io/legal/"),
                        Contact = new Microsoft.OpenApi.Models.OpenApiContact
                        {
                            Email = "",
                            Url = new Uri("https://tangrams.io/about-tangram/team/")
                        }
                    });
                });

                services.AddHttpContextAccessor()
                    .AddSingleton<ISafeguardDownloadingFlagProvider, SafeguardDownloadingFlagProvider>()
                    .AddHostedService<SafeguardService>()
                    .AddSingleton<IWalletService, WalletService>()
                    .AddOptions();
            }

            public void Configure(IApplicationBuilder app)
            {
                var pathBase = Configuration["PATH_BASE"];
                if (!string.IsNullOrEmpty(pathBase))
                    app.UsePathBase(pathBase);

                app.UseSwagger()
                    .UseSwaggerUI(c =>
                    {
                        c.SwaggerEndpoint($"{ (!string.IsNullOrEmpty(pathBase) ? pathBase : string.Empty) }/swagger/v1/swagger.json", "BAMWalletRest.API V1");
                        c.OAuthClientId("walletrestswaggerui");
                        c.OAuthAppName("Bamboo Wallet Rest Swagger UI");
                    })
                    .UseStaticFiles()
                    .UseRouting()
                    .UseEndpoints(endpoints =>
                    {
                        endpoints.MapControllerRoute(
                            name: "default",
                            pattern: "{controller=Home}/{action=Index}/{id?}");
                    });
            }
        }

        public bool WebServerRunning { get; }
        public string Advertise { get; }

        public SelfHosted(IConfiguration configuration)
        {
            var networkSection = configuration.GetSection(Constant.Network);
            WebServerRunning = networkSection.GetValue<bool>(Constant.RunAsWebServer);
            Advertise = networkSection.GetValue<string>(Constant.Advertise);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (WebServerRunning)
            {
                WebHost.CreateDefaultBuilder()
                    .UseKestrel()
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .UseUrls(Advertise)
                    .UseStartup<Startup>()
                    .Build().RunAsync(stoppingToken);
            }
            return Task.CompletedTask;
        }
    }
}
