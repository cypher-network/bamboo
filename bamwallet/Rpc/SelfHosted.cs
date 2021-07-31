// BAMWallet by Matthew Hellyer is licensed under CC BY-NC-ND 4.0. 
// To view a copy of this license, visit https://creativecommons.org/licenses/by-nc-nd/4.0

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using BAMWallet.HD;
using BAMWallet.Model;
using BAMWallet.Rpc.Formatters;
using BAMWallet.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;

namespace BAMWallet.Rpc
{
    public class SelfHosted : BackgroundService
    {
        private class Startup
        {
            /// <summary>
            /// 
            /// </summary>
            /// <param name="configuration"></param>
            public Startup(IConfiguration configuration)
            {
                Configuration = configuration;
            }

            /// <summary>
            /// 
            /// </summary>
            public IConfiguration Configuration { get; }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="services"></param>
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

                services
                    .AddHttpContextAccessor()
                    .AddOptions()
                    .AddSingleton(Log.Logger)
                    .Configure<NetworkSettings>(options => Configuration.GetSection("NetworkSettings").Bind(options))
                    .AddSingleton<ISafeguardDownloadingFlagProvider, SafeguardDownloadingFlagProvider>()
                    .AddHostedService<SafeguardService>()
                    .AddSingleton<IWalletService, WalletService>();

                services.AddLogging(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders();
                });
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="app"></param>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="networkSettings"></param>
        public SelfHosted(IOptions<NetworkSettings> networkSettings)
        {
            _networkSettings = networkSettings.Value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var builder = CreateWebHostBuilder();
            using var host = builder.Build();
            await host.RunAsync(token: stoppingToken);
            await host.WaitForShutdownAsync(token: stoppingToken);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private IHostBuilder CreateWebHostBuilder() =>
            Host.CreateDefaultBuilder(null)
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>()
                        .UseUrls(_networkSettings.WebserverEndpoint);
                });
    }
}