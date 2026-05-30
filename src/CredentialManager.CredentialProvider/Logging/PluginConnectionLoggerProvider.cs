using System.Collections.Concurrent;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CredentialManager.CredentialProvider.Logging;

[ProviderAlias("PluginConnection")]
internal sealed class PluginConnectionLoggerProvider : ILoggerProvider
{
    private readonly IDisposable? _onChangeToken;
    private PluginConnectionLoggerOptions _config;
    private readonly ConcurrentDictionary<string, PluginConnectionLogger> _loggers = [];
    private readonly IPluginConnectionProvider _pluginConnectionProvider;

    public PluginConnectionLoggerProvider(
        IOptionsMonitor<PluginConnectionLoggerOptions> config,
        IPluginConnectionProvider pluginConnectionProvider
    )
    {
        _config = config.CurrentValue;
        _onChangeToken = config.OnChange(updatedConfig => _config = updatedConfig);
        _pluginConnectionProvider = pluginConnectionProvider;
    }

    public ILogger CreateLogger(string categoryName)
        => _loggers.GetOrAdd(categoryName,
            valueFactory: _ => new PluginConnectionLogger(() => _config, _pluginConnectionProvider));

    public void Dispose()
    {
        _loggers.Clear();
        _onChangeToken?.Dispose();
    }
}
