using System;
using BAMWallet.HD;
using BAMWallet.Model;
using BAMWallet.Rpc;
using BAMWallet.Services;
using CLi.ApplicationLayer.Commands;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Serilog;

namespace Cli
{
    public class Startup
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
                services.AddSingleton<IHostedService, SelfHosted>(sp =>
                {
                    var selfHosted = new SelfHosted(sp.GetService<IOptions<NetworkSettings>>());
                    return selfHosted;
                });
            }

            services
                .AddSingleton<ISafeguardDownloadingFlagProvider, SafeguardDownloadingFlagProvider>()
                .AddHostedService<SafeguardService>().AddSingleton<IWalletService, WalletService>()
                .AddSingleton<ICommandService, CommandService>()
                .AddSingleton<IHostedService, CommandService>(sp => sp.GetService<ICommandService>() as CommandService)
                .Add(new ServiceDescriptor(typeof(IConsole), PhysicalConsole.Singleton));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="app"></param>
        /// <param name="lifetime"></param>
        public void Configure(IApplicationBuilder app, IHostApplicationLifetime lifetime)
        {

        }
    }
}