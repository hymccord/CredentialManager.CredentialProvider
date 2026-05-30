using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;

namespace CredentialManager.CredentialProvider.Logging;

public static class PluginConnectionLoggerExtensions
{
    extension (ILoggingBuilder builder)
    {
        public ILoggingBuilder AddPluginConnectionLogger()
        {
            builder.AddConfiguration();

            builder.Services.TryAddEnumerable(
                ServiceDescriptor.Singleton<ILoggerProvider, PluginConnectionLoggerProvider>());

            LoggerProviderOptions.RegisterProviderOptions
                <PluginConnectionLoggerOptions, PluginConnectionLoggerProvider>(builder.Services);

            return builder;

        }

        public ILoggingBuilder AddPluginConnectionLogger(Action<PluginConnectionLoggerOptions> configure)
        {
            builder.AddPluginConnectionLogger();
            builder.Services.Configure(configure);

            return builder;
        }
    }
}
