using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzureLogsCleaner;

internal abstract class AbstractWorker<T> : BackgroundService
    where T: AbstractWorker<T>
{
    protected ILogger<T> Logger;
    protected IHostApplicationLifetime ApplicationLifetime;

    private readonly bool _isVitalService;
    private readonly TimeSpan _delay;

    protected AbstractWorker(ILogger<T> logger, TimeSpan delay, IHostApplicationLifetime applicationLifetime,
        bool isVitalService = false)
    {
        ApplicationLifetime = applicationLifetime ?? throw new ArgumentNullException(nameof(applicationLifetime));
        _isVitalService = isVitalService;

        Logger = logger;
        _delay = delay;
    }

    protected virtual Task PreExecuteAsyncInternalAsync(CancellationToken stoppingToken)
    {
        // Override in derived class
        return Task.CompletedTask;
    }

    protected virtual Task ExecuteAsyncExceptionHandlerAsync(Exception ex, CancellationToken stoppingToken)
    {
        // Override in derived class
        return Task.CompletedTask;
    }

    protected abstract Task ExecuteInternalAsync(CancellationToken stoppingToken);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Logger.LogInformation("Execution starting...");

        using var cts =
            CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, ApplicationLifetime.ApplicationStopping);
        await PreExecuteAsyncInternalAsync(cts.Token).ConfigureAwait(false);

        await Task.Yield();

        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                await ExecuteInternalAsync(cts.Token)
                    .ConfigureAwait(false);

                Logger.LogInformation("Work cycle finished. Next in {delay}", _delay);

                await Task.Delay(_delay, cts.Token)
                    .ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            Logger.LogWarning("Execution has been cancelled.");
        }
        catch (Exception ex)
        {
            Logger.LogCritical(ex.ToString());

            await ExecuteAsyncExceptionHandlerAsync(ex, cts.Token).ConfigureAwait(false);
        }
        finally
        {
            Logger.LogInformation("Execution stopped.");
            if (_isVitalService)
            {
                ApplicationLifetime.StopApplication();
            }
        }
    }
}