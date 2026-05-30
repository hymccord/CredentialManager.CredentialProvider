using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NuGet.Protocol.Plugins;

namespace CredentialManager.CredentialProvider.RequestHandlers;

internal class SetCredentialsRequestHandler(ILogger<SetCredentialsRequestHandler> logger, IHostApplicationLifetime hostApplicationLifetime)
    : RequestHandlerBase<SetCredentialsRequest, SetCredentialsResponse>(logger, hostApplicationLifetime)
{
    private static readonly SetCredentialsResponse SuccessResponse = new(MessageResponseCode.Success);

    public override MessageMethod Method => MessageMethod.SetCredentials;

    protected override Task<SetCredentialsResponse?> HandleRequestAsync(SetCredentialsRequest? request)
    {
        return Task.FromResult<SetCredentialsResponse?>(SuccessResponse);
    }
}