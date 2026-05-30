using NuGet.Protocol.Plugins;

namespace CredentialManager.CredentialProvider.RequestHandlers;

public interface IMessageRequestHandler : IRequestHandler
{
    MessageMethod Method { get; }
}
