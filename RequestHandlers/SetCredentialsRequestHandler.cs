using Microsoft.Extensions.Logging;

using NuGet.Protocol.Plugins;

namespace CredentialManager.CredentialProvider.RequestHandlers;

internal class SetCredentialsRequestHandler(ILogger logger) : RequestHandlerBase<SetCredentialsRequest, SetCredentialsResponse>(logger)
{
    private static readonly SetCredentialsResponse SuccessResponse = new(MessageResponseCode.Success);
    protected override Task<SetCredentialsResponse?> HandleRequestAsync(SetCredentialsRequest? request)
    {
        return Task.FromResult<SetCredentialsResponse?>(SuccessResponse);
    }
}