using Microsoft.Extensions.Logging;

using NuGet.Protocol.Plugins;

namespace CredentialManager.CredentialProvider.RequestHandlers;

internal class SetLogLevelRequestHandler(ILogger logger) : RequestHandlerBase<SetLogLevelRequest, SetLogLevelResponse>(logger)
{
    private static readonly SetLogLevelResponse SuccessResponse = new(MessageResponseCode.Success);

    protected override Task<SetLogLevelResponse?> HandleRequestAsync(SetLogLevelRequest? request)
    {
        return Task.FromResult<SetLogLevelResponse?>(SuccessResponse);
    }
}
