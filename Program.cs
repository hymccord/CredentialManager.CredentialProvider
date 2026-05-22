using System.Diagnostics;

using CredentialManager.CredentialProvider.CredentialProviders;
using CredentialManager.CredentialProvider.RequestHandlers;

using Microsoft.Extensions.Logging;

using NReco.Logging.File;

using NuGet.Protocol.Plugins;

namespace CredentialManager.CredentialProvider;

public static class Program
{
    private static bool _shuttingDown = false;
    public static bool IsShuttingDown = Volatile.Read(ref _shuttingDown);

    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args[0] != "-Plugin")
        {
            Console.WriteLine("This application is meant to be used as a plugin for NuGet.");
            return -2;

        }

        CancellationTokenSource cts = new();

        using var loggerFactory = LoggerFactory.Create(b =>
        {
            if (Environment.GetEnvironmentVariable("CREDENTIALMANAGER_CREDENTIALPROVIDER_LOG_PATH") is string logPath)
            {
                b.AddFile(logPath, append: true);
                b.AddDebug();
            }

            b.SetMinimumLevel(LogLevel.Debug);
        });
        var logger = loggerFactory.CreateLogger(nameof(Program));

        if (Environment.GetEnvironmentVariable("CREDENTIALMANAGER_CREDENTIALPROVIDER_DEBUG") == "1")
        {
            logger.LogInformation("Launching debugger...");
            Debugger.Launch();
        }

        List<ICredentialProvider> credentialProviders = [
            new WindowsCredentialProvider(),
        ];

        try
        {
            IRequestHandlers requestHandlers = new RequestHandlerCollection
            {
                { MessageMethod.GetAuthenticationCredentials, new GetAuthenticationCredentialsRequestHandler(logger, credentialProviders, cts.Token) },
                { MessageMethod.GetOperationClaims, new GetOperationClaimsRequestHandler(logger, credentialProviders) },
                { MessageMethod.Initialize, new InitializeRequestHandler(logger) },
                { MessageMethod.SetLogLevel, new SetLogLevelRequestHandler(logger) },
                { MessageMethod.SetCredentials, new SetCredentialsRequestHandler(logger) }
            };

            try
            {
                using IPlugin plugin = await PluginFactory.CreateFromCurrentProcessAsync(requestHandlers, ConnectionOptions.CreateDefault(), cts.Token);
                await WaitForPluginExitAsync(plugin, logger, TimeSpan.FromMinutes(2)).ConfigureAwait(false);

            }
            catch (OperationCanceledException ex)
            {
                logger.LogTrace(ex.ToString());
            }

            return 0;
        }
        finally
        {
            foreach (var provider in credentialProviders)
            {
                provider.Dispose();
            }
        }
    }

    private static async Task WaitForPluginExitAsync(IPlugin plugin, ILogger logger, TimeSpan timeout)
    {
        var beginShutdownTaskSource = new TaskCompletionSource<object?>();
        var endShutdownTaskSource = new TaskCompletionSource<object?>();

        plugin.Connection.Faulted += (s, e)
            => logger.LogError(e.Exception, "Faulted on message: {Type} {Method} {Id}", e.Message.Type, e.Message.Method, e.Message.RequestId);

        plugin.BeforeClose += (s, e) =>
        {
            Volatile.Write(ref _shuttingDown, true);
            beginShutdownTaskSource.TrySetResult(null);
        };

        plugin.Closed += (s, e) =>
        {
            beginShutdownTaskSource.TrySetResult(null);
            endShutdownTaskSource.TrySetResult(null);
        };

        await beginShutdownTaskSource.Task.ConfigureAwait(false);

        using (new Timer(_ => endShutdownTaskSource.TrySetCanceled(), null, timeout, TimeSpan.FromMilliseconds(-1)))
        {
            await endShutdownTaskSource.Task.ConfigureAwait(false);
        }

        if (endShutdownTaskSource.Task.IsCanceled)
        {
            logger.LogError("Timed out waiting for plug-in operatios to complete.");
        }
    }
}
