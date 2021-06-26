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
using BAMWallet.Model;
using BAMWallet.Rpc.Formatters;
using BAMWallet.Services;
using Microsoft.Extensions.Options;

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

                services.AddHttpContextAccessor().AddOptions()
                    .Configure<NetworkSettings>(options => Configuration.GetSection("NetworkSettings").Bind(options))
                    .AddSingleton<ISafeguardDownloadingFlagProvider, SafeguardDownloadingFlagProvider>()
                    .AddHostedService<SafeguardService>()
                    .AddSingleton<IWalletService, WalletService>();
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

        private readonly NetworkSettings _networkSettings;

        public SelfHosted(IOptions<NetworkSettings> networkSettings)
        {
            _networkSettings = networkSettings.Value;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (_networkSettings.RunAsWebServer)
            {
                WebHost.CreateDefaultBuilder()
                    .UseKestrel()
                    .UseContentRoot(Directory.GetCurrentDirectory())
                    .UseUrls(_networkSettings.Advertise)
                    .UseStartup<Startup>()
                    .Build().RunAsync(stoppingToken);
            }
            return Task.CompletedTask;
        }
    }
}
