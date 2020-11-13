using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzureLogsCleaner
{
    internal class Worker : AbstractWorker<Worker>
    {
        private readonly AzureClientInitializer _azureClientInitializer;

        public Worker(
            AzureClientInitializer azureClientInitializer,
            ILogger<Worker> logger,
            IHostApplicationLifetime applicationLifetime)
            : base(logger, Timeout.InfiniteTimeSpan, applicationLifetime, true)
        {
            _azureClientInitializer = azureClientInitializer
                                      ?? throw new ArgumentNullException(nameof(azureClientInitializer));
        }

        protected override async Task ExecuteInternalAsync(CancellationToken stoppingToken)
        {
            var analyzer = await _azureClientInitializer.CreateAnalyzerAsync()
                .ConfigureAwait(false);

            if (!DateTime.TryParse("2020-01-01", out var threshold))
                return;

            var count = await analyzer.DoAsync(threshold).ConfigureAwait(false);

            Logger.LogInformation($"{count} records are older that {threshold}");
        }
    }
}
