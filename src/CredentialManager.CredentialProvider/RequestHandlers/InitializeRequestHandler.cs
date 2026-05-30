using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NuGet.Protocol.Plugins;

namespace CredentialManager.CredentialProvider.RequestHandlers;

internal class InitializeRequestHandler(ILogger<InitializeRequestHandler> logger, IHostApplicationLifetime hostApplicationLifetime)
    : RequestHandlerBase<InitializeRequest, InitializeResponse>(logger, hostApplicationLifetime)
{
    public override MessageMethod Method => MessageMethod.Initialize;

    protected override Task<InitializeResponse?> HandleRequestAsync(InitializeRequest? request)
    {
        return Task.FromResult<InitializeResponse?>(new InitializeResponse(MessageResponseCode.Success));
    }
}