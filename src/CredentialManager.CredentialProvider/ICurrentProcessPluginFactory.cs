using CredentialManager.CredentialProvider.RequestHandlers;

using NuGet.Protocol.Plugins;

namespace CredentialManager.CredentialProvider;

public interface ICurrentProcessPluginFactory
{
    Task<IPlugin> CreateAsync(IEnumerable<IMessageRequestHandler> requestHandlers, ConnectionOptions connectionOptions, CancellationToken cancellationToken);
}

public class CurrentProcessPluginFactory : ICurrentProcessPluginFactory
{
    public Task<IPlugin> CreateAsync(IEnumerable<IMessageRequestHandler> requestHandlers, ConnectionOptions connectionOptions, CancellationToken cancellationToken)
    {
        var requestHandlerCollection = new RequestHandlerCollection();
        foreach (var handler in requestHandlers)
        {
            requestHandlerCollection.Add(handler.Method, handler);
        }

        return PluginFactory.CreateFromCurrentProcessAsync(requestHandlerCollection, connectionOptions, cancellationToken);
    }
}