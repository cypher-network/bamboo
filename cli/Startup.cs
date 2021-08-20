using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BAMWallet.HD;
using BAMWallet.Helper;
using BAMWallet.Model;
using BAMWallet.Rpc.Formatters;
using BAMWallet.Services;
using Cli.Commands.Common;
using McMaster.Extensions.CommandLineUtils;
using Serilog;
using System.Threading;
using System.Threading.Tasks;

namespace Cli
{
    public class Startup : BackgroundService
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="env"></param>
        /// <param name="configuration"></param>
        public Startup(IWebHostEnvironment env, IConfiguration configuration)
        {
            Configuration = configuration;
        }

        /// <summary>
        ///
        /// </summary>
        public IConfiguration Configuration { get; private set; }

        /// <summary>
        ///
        ///
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureServices(IServiceCollection services)
        {
            var ranAsWebServer = Convert.ToBoolean(Configuration["NetworkSettings:RunAsWebServer"]);
            services.AddOptions()
                .Configure<NetworkSettings>(options => Configuration.GetSection("NetworkSettings").Bind(options))
                .Configure<TimingSettings>(options => Configuration.GetSection("Timing").Bind(options))
                .AddSingleton(Log.Logger);

            if (ranAsWebServer)
            {
                services.AddMvcCore().AddApiExplorer();
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
                        Version = Util.GetAssemblyVersion(),
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
                    .AddSingleton(Log.Logger)
                    .AddSingleton<ISafeguardDownloadingFlagProvider, SafeguardDownloadingFlagProvider>()
                    .AddHostedService<SafeguardService>()
                    .AddSingleton<ICommandReceiver, CommandReceiver>()
                    .AddSingleton<ICommandService, CommandInvoker>()
                    .AddSingleton<IHostedService, CommandInvoker>(sp => sp.GetService<ICommandService>() as CommandInvoker)
                    .Add(new ServiceDescriptor(typeof(IConsole), PhysicalConsole.Singleton));

                services.AddLogging(loggingBuilder =>
                {
                    loggingBuilder.ClearProviders();
                });
            }
            else
            {
                services.Add(new ServiceDescriptor(typeof(IConsole), PhysicalConsole.Singleton));
                services
                    .AddSingleton<ISafeguardDownloadingFlagProvider, SafeguardDownloadingFlagProvider>()
                    .AddHostedService<SafeguardService>()
                    .AddSingleton<ICommandReceiver, CommandReceiver>()
                    .AddSingleton<ICommandService, CommandInvoker>()
                    .AddSingleton<IHostedService, CommandInvoker>(sp => sp.GetService<ICommandService>() as CommandInvoker);

            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="app"></param>
        /// <param name="lifetime"></param>
        public void Configure(IApplicationBuilder app, IHostApplicationLifetime lifetime)
        {
            var ranAsWebServer = Convert.ToBoolean(Configuration["NetworkSettings:RunAsWebServer"]);
            if (ranAsWebServer)
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
                        .UseUrls(Configuration.GetSection("NetworkSettings").GetValue<string>("WebserverEndpoint"));
                });
    }
}