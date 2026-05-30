using NuGet.Protocol.Plugins;

namespace CredentialManager.CredentialProvider.Logging;

internal interface IPluginConnectionProvider
{
    IConnection? Connection { get; }
}

public class LoggingPluginConnectionProvider() : IPluginConnectionProvider
{
    public IConnection? Connection { get; private set; }

    public void SetConnection(IConnection? connection)
    {
        Connection = connection;
    }
}