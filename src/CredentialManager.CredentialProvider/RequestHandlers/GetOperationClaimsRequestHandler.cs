using CredentialManager.CredentialProvider.CredentialProviders;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NuGet.Protocol.Plugins;

namespace CredentialManager.CredentialProvider.RequestHandlers;

internal class GetOperationClaimsRequestHandler : RequestHandlerBase<GetOperationClaimsRequest, GetOperationClaimsResponse>
{
    private static readonly GetOperationClaimsResponse CanProvideCredentialsResponse = new([
        OperationClaim.Authentication
        ]);

    private static readonly GetOperationClaimsResponse EmptyGetOperationsClaimResponse = new([]);

    private readonly IReadOnlyCollection<ICredentialProvider> _credentialProviders;

    public GetOperationClaimsRequestHandler(ILogger<GetOperationClaimsRequestHandler> logger,
        IEnumerable<ICredentialProvider> credentialProviders,
        IHostApplicationLifetime hostApplicationLifetime)
        : base(logger, hostApplicationLifetime)
    {
        ArgumentNullException.ThrowIfNull(credentialProviders);

        _credentialProviders = [..credentialProviders];
    }

    public override MessageMethod Method => MessageMethod.GetOperationClaims;

    protected override Task<GetOperationClaimsResponse?> HandleRequestAsync(GetOperationClaimsRequest? request)
    {
        if (request?.PackageSourceRepository != null || request?.ServiceIndex != null)
        {
            return Task.FromResult<GetOperationClaimsResponse?>(EmptyGetOperationsClaimResponse);
        }

        return Task.FromResult<GetOperationClaimsResponse?>(CanProvideCredentialsResponse);
    }
}
