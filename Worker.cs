using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ethermine_monitor
{
    public class Worker : BackgroundService
    {
        private readonly ILogger _logger;
        private readonly IMessageWriter _messageWriter;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;
        private readonly AppSettings _options;
        private DateTime _lastAlert;
        private Alerts _alerts;
        private string _name;
        private string _email;

        public Worker(
            IConfiguration configuration,
            IMessageWriter messageWriter,
            IEmailSender emailSender,
            ILogger<Worker> logger,
            IOptions<AppSettings> options,
            IHostApplicationLifetime appLifetime)
        {
            _configuration = configuration;
            _logger = logger;
            _options = options.Value;
            _messageWriter = messageWriter;
            _emailSender = emailSender;

            _name = _options.SmtpConfig.Name;
            _email = _options.SmtpConfig.EmailAddress;

            _lastAlert = DateTime.Now.AddHours(_options.Alerts.Frequency*-1);
            appLifetime.ApplicationStarted.Register(OnStarted);
            appLifetime.ApplicationStopping.Register(OnStopping);
            appLifetime.ApplicationStopped.Register(OnStopped);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {           
            _messageWriter.Write($"Worker runs every minute");

            while (!stoppingToken.IsCancellationRequested)
            {
                _messageWriter.Write($"Worker running at: {DateTimeOffset.Now}");
                // call api
                var apiClient = new HttpClient();

                await GetData(apiClient);
                //await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                await Task.Delay(10000, stoppingToken);

            }

        }

        private async Task GetData(HttpClient apiClient)
        {
            try
            {               
                var miner = _options.Alerts.Miner;
                var url = $"https://api.ethermine.org/miner/{miner}/dashboard";
                var response = await apiClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    var errorMsg = $"HTTP status code {response.StatusCode}";
                    _messageWriter.Write(errorMsg);
                    await SendAlert(_name, _email, "Error connecting to the pool", errorMsg, Alerts.Error);
                    
                }
                else
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var options = new JsonSerializerOptions
                    {
                        IncludeFields = true,
                    };
                    var result = JsonSerializer.Deserialize<RootObject<Dashboard>>(content, options);
                    var workersAlert = _options.Alerts.Workers;
                    var workersHttp = result.data.workers;
                    if (_options.Alerts.WorkersWatch && workersHttp.Count < workersAlert)
                    {
                        var msg = $"Total workers online is less than {workersAlert}";
                        _messageWriter.Write(msg, false);
                        await SendAlert(_name, _email, $"{workersHttp.Count} Workers online", msg, Alerts.OK);
                    }

                    var totalAlert = _options.Alerts.Speed;
                    var totalHttp = result.data.currentStatistics.currentHashrate /1000000;
                    if (_options.Alerts.SpeedWatch && totalHttp < totalAlert)
                    {
                        var msg = $"Total workers speed is less than {totalAlert}mh/h";
                        _messageWriter.Write(msg, false);
                        await SendAlert(_name, _email, $"Current workers speed:{totalHttp}mh/h", msg, Alerts.OK);
                    }

                    if (_options.Alerts.PrintStatus)
                    {
                        _messageWriter.Write(content);
                    }
                   
                }
               
            }
            catch (Exception ex)
            {
                var errorMsg = $"Exception: {ex.Message}";
                _messageWriter.Write(errorMsg);
                await SendAlert(_name, _email, "Exception error", errorMsg, Alerts.Error);
            }
        }

        private async Task SendAlert(
            string recepientName,
            string recepientEmail,
            string subject,
            string body,
            Alerts alert)
        {
            if (_lastAlert <= DateTime.Now || _alerts == alert)
            {
                await _emailSender.SendEmailAsync(recepientName, recepientEmail, subject, body);
                _lastAlert = DateTime.Now.AddHours(_options.Alerts.Frequency* -1);
            }
            _alerts = alert;
        }

        private async void OnStarted()
        {
            var msg = $"Started at:{DateTime.Now}";
            _messageWriter.Write(msg);
            if (_options.Alerts.AlertOnStop)
            {
                await SendAlert(_name, _email, $"Alerts started", msg, Alerts.OK);
            }

        }

        private void OnStopping()
        {
            _logger.LogInformation("OnStopping has been called.");
        }

        private async void OnStopped()
        {
            var msg = $"Stoped at:{DateTime.Now}";
            _messageWriter.Write(msg);
            if (_options.Alerts.AlertOnStop)
            {               
                await SendAlert(_name, _email, $"Alerts stopped", msg, Alerts.OK);
            }
        }
    }

    internal enum Alerts
    {
        OK,
        Error
    }
}