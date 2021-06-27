using BAMWallet.HD;
using BAMWallet.Model;
using BAMWallet.Services;
using CLi.ApplicationLayer.Commands;
using McMaster.Extensions.CommandLineUtils;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions()
                .Configure<NetworkSettings>(options => Configuration.GetSection("NetworkSettings").Bind(options))
                .AddSingleton<ISafeguardDownloadingFlagProvider, SafeguardDownloadingFlagProvider>()
                .AddHostedService<SafeguardService>().AddSingleton<IWalletService, WalletService>()
                .AddSingleton<ICommandService, CommandService>()
                .AddSingleton<IHostedService, BAMWallet.Rpc.SelfHosted>()
                .AddSingleton<IHostedService, CommandService>(sp => sp.GetService<ICommandService>() as CommandService)
                .AddSingleton(Log.Logger)
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