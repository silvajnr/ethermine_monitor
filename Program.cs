using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO;

namespace ethermine_monitor
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using IHost host = CreateHostBuilder(args).Build();           

            await host.RunAsync();
        }

        static IHostBuilder CreateHostBuilder(string[] args) =>
                Host.CreateDefaultBuilder(args)
                //.ConfigureAppConfiguration((hostingContext, configuration) =>
                //{
                //    IHostEnvironment env = hostingContext.HostingEnvironment;
                //    var oo = Directory.GetCurrentDirectory();
                //    IConfigurationRoot configurationRoot = configuration.Build();

                //    TransientFaultHandlingOptions options = new();
                //    configurationRoot.GetSection(nameof(TransientFaultHandlingOptions))
                //                     .Bind(options);

                //    Console.WriteLine($"TransientFaultHandlingOptions.Enabled={options.Enabled}");
                //    Console.WriteLine($"TransientFaultHandlingOptions.AutoRetryDelay={options.AutoRetryDelay}");
                //})
                .ConfigureServices((context, services) => {

                    services.AddHostedService<Worker>()
                        .AddScoped<IMessageWriter, MessageWriter>()
                        .AddScoped<IEmailSender, EmailSender>();

                    services.Configure<AppSettings>(context.Configuration);
                })
                .ConfigureLogging((hostingContext, logging) =>
                {
                    logging.ClearProviders();
                    logging.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    logging.AddConsole();
                    logging.AddDebug();
                    logging.AddEventSourceLogger();
                    logging.AddFile(hostingContext.Configuration.GetSection("Logging"));
                });

    }

    public class AppSettings
    {
        public AlertsOptions Alerts { get; set; }
        public SmtpConfig SmtpConfig { get; set; }

    }

    public class AlertsOptions
    {
        public string Miner { get; set; }
        public int Speed { get; set; }
        public bool SpeedWatch { get; set; }
        public int Workers { get; set; }
        public bool WorkersWatch { get; set; }
        public double Frequency { get; set; }
        public bool PrintStatus { get; set; }
        public bool AlertOnStart { get; set; }
        public bool AlertOnStop { get; set; }
    }
    
    public class RootObject<T>
    {
        public string status { get; set; }
        public T data { get; set; }
    }

    public class RootObjects<T>
    {
        public string status { get; set; }
        public IList<T> data { get; set; }
    }

    public class Dashboard
    {
        public IList<Statistic> statistics { get; set; }
        public IList<Miner> workers { get; set; }
        public Currentstatistics currentStatistics { get; set; }
        public Settings settings { get; set; }
    }

    public class Base
    {
        public int time { get; set; }
        public int reportedHashrate { get; set; }
        public float currentHashrate { get; set; }
        public int validShares { get; set; }
        public int invalidShares { get; set; }
        public int staleShares { get; set; }
        public float averageHashrate { get; set; }
    }

    public class Currentstatistics: History
    {
        public long unpaid { get; set; }
    }

    public class Settings
    {
        public string email { get; set; }
        public int monitor { get; set; }
        public long minPayout { get; set; }
        public string ip { get; set; }
    }

    public class Statistic : Base
    {
        public int activeWorkers { get; set; }
    }

    public class Miner: History
    {
        public string worker { get; set; }
    }
    
    public class History: Statistic
    {
        public int lastSeen { get; set; }
    }


}
