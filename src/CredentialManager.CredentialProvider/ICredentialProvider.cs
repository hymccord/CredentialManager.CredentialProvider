using NuGet.Protocol.Plugins;

namespace CredentialManager.CredentialProvider;

internal interface ICredentialProvider : IDisposable
{
    Task<bool> CanProvideCredentialAsync(Uri uri);

    Task<GetAuthenticationCredentialsResponse?> HandleRequestAsync(GetAuthenticationCredentialsRequest request, CancellationToken cancellationToken);

    bool IsCachable { get; }
}