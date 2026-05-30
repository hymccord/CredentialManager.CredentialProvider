using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

using NuGet.Protocol.Plugins;

namespace CredentialManager.CredentialProvider.Logging;

public sealed class PluginConnectionLoggerOptions
{
    public LogLevel MinLevel { get; set; } = LogLevel.Warning;
}

internal sealed class PluginConnectionLogger(
    Func<PluginConnectionLoggerOptions> getCurrentConfig,
    IPluginConnectionProvider pluginConnectionProvider
    ) : ILogger
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => default!;

    public bool IsEnabled(LogLevel logLevel)
        => logLevel >= getCurrentConfig().MinLevel;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        if (pluginConnectionProvider.Connection is null)
        {
            return;
        }

        pluginConnectionProvider.Connection
            .SendRequestAndReceiveResponseAsync<LogRequest, LogResponse>(
                MessageMethod.Log,
                new LogRequest(
                    logLevel.ToNugetLogLevel(),
                    message: $"[CredentialManager] {formatter(state, exception)}"),
                CancellationToken.None)
            .ContinueWith(x => x.Exception, TaskContinuationOptions.OnlyOnFaulted);
    }
}

file static class LogLevelExtensions
{
    public static NuGet.Common.LogLevel ToNugetLogLevel(this LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => NuGet.Common.LogLevel.Verbose,
            LogLevel.Debug => NuGet.Common.LogLevel.Verbose,
            LogLevel.Information => NuGet.Common.LogLevel.Information,
            LogLevel.Warning => NuGet.Common.LogLevel.Warning,
            LogLevel.Error => NuGet.Common.LogLevel.Error,
            LogLevel.Critical => NuGet.Common.LogLevel.Error,
            _ => NuGet.Common.LogLevel.Information
        };
    }
}
