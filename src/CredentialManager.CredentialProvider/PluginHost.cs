using CredentialManager.CredentialProvider.Logging;
using CredentialManager.CredentialProvider.RequestHandlers;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NuGet.Protocol.Plugins;

namespace CredentialManager.CredentialProvider;

internal class PluginHost(
    ILogger<PluginHost> logger,
    ICurrentProcessPluginFactory currentProcessPluginFactory,
    IEnumerable<IMessageRequestHandler> requestHandlers,
    IHostApplicationLifetime applicationLifetime,
    LoggingPluginConnectionProvider loggingPluginConnectionProvider) : BackgroundService
{

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        IPlugin plugin;
        try
        {
            logger.LogDebug("Running in plugin mode.");
            logger.LogDebug(PlatformInformation.GetProgramVersion());
            plugin = await currentProcessPluginFactory.CreateAsync(requestHandlers, ConnectionOptions.CreateDefault(), stoppingToken);
            loggingPluginConnectionProvider.SetConnection(plugin.Connection);
        }
        catch (OperationCanceledException)
        {
            applicationLifetime.StopApplication();
            return;
        }

        try
        {
            await WaitForPluginExitAsync(plugin, stoppingToken).ConfigureAwait(false);
            loggingPluginConnectionProvider.SetConnection(null);
        }
        finally
        {
            plugin.Dispose();
        }
    }

    private async Task WaitForPluginExitAsync(IPlugin plugin, CancellationToken stoppingToken)
    {
        var closedTcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        plugin.Connection.Faulted += (s, e)
            => logger.LogError(e.Exception, "Faulted on message: {Type} {Method} {Id}", e.Message.Type, e.Message.Method, e.Message.RequestId);

        plugin.BeforeClose += (s, e) =>
        {
            applicationLifetime.StopApplication();
        };

        plugin.Closed += (s, e) => closedTcs.TrySetResult();

        try
        {
            // Wait for the plugin to close cleanly, or for the host to begin stopping.
            await closedTcs.Task.WaitAsync(stoppingToken).ConfigureAwait(false);
            return;
        }
        catch (OperationCanceledException) { }

        // stoppingToken was cancelled (from BeforeClose or an external signal).
        // Give the plugin up to 2 minutes to finish closing.
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        try
        {
            await closedTcs.Task.WaitAsync(timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            logger.LogError("Timed out waiting for plug-in operations to complete.");
        }
    }
}
