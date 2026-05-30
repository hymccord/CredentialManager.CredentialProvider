using System.Diagnostics;

using CredentialManager.CredentialProvider.CredentialProviders;
using CredentialManager.CredentialProvider.Logging;
using CredentialManager.CredentialProvider.RequestHandlers;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NReco.Logging.File;

namespace CredentialManager.CredentialProvider;

public class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0 || args[0] != "-Plugin")
        {
            Console.WriteLine("This application is meant to be used as a plugin for NuGet.");
            return -2;

        }

        if (Environment.GetEnvironmentVariable("CREDENTIALMANAGER_CREDENTIALPROVIDER_DEBUG") == "1")
        {
            Debugger.Launch();
        }

        var builder = Host.CreateApplicationBuilder();

        // NuGet plugins communicate via standard in and out,
        // We don't want any other providers that might write to those streams.
        builder.Logging.ClearProviders();
        if (builder.Configuration.GetValue<string>("CREDENTIALMANAGER_CREDENTIALPROVIDER_LOG_PATH") is string logPath)
        {
            //builder.Logging.AddFilter("Microsoft.Hosting", LogLevel.Warning);
            builder.Logging.AddFilter<FileLoggerProvider>(null, LogLevel.Trace);
            builder.Logging.AddFile(logPath, c =>
            {
                c.Append = true;
                c.MinLevel = LogLevel.Debug;
            });

            builder.Logging.AddPluginConnectionLogger(c => c.MinLevel = LogLevel.Warning);
        }

        builder.Services.AddSingleton<LoggingPluginConnectionProvider>();
        builder.Services.AddSingleton<IPluginConnectionProvider>(sp => sp.GetRequiredService<LoggingPluginConnectionProvider>());
        builder.Services.AddSingleton<ICurrentProcessPluginFactory, CurrentProcessPluginFactory>();

        builder.Services.AddTransient<ICredentialProvider, WindowsCredentialProvider>();

        builder.Services.AddTransient<IMessageRequestHandler, GetAuthenticationCredentialsRequestHandler>();
        builder.Services.AddTransient<IMessageRequestHandler, GetOperationClaimsRequestHandler>();
        builder.Services.AddTransient<IMessageRequestHandler, InitializeRequestHandler>();
        builder.Services.AddTransient<IMessageRequestHandler, SetLogLevelRequestHandler>();
        builder.Services.AddTransient<IMessageRequestHandler, SetCredentialsRequestHandler>();

        builder.Services.AddHostedService<PluginHost>();

        var host = builder.Build();
        try
        {
            await host.RunAsync();
        }
        catch (OperationCanceledException)
        {

        }
        catch
        {
            host.Services.GetRequiredService<ILogger<Program>>().LogError("Plugin host terminated unexpectedly.");
            return -1;
        }

        return 0;
    }

}