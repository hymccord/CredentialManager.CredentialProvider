using Microsoft.Extensions.Logging;

using NuGet.Protocol.Plugins;

namespace CredentialManager.CredentialProvider.RequestHandlers;

internal class InitializeRequestHandler : RequestHandlerBase<InitializeRequest, InitializeResponse>
{
    public InitializeRequestHandler(ILogger logger)
        : base(logger)
    {
    }
    protected override Task<InitializeResponse?> HandleRequestAsync(InitializeRequest? request)
    {
        return Task.FromResult<InitializeResponse?>(new InitializeResponse(MessageResponseCode.Success));
    }
}