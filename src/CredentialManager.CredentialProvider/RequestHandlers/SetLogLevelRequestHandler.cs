using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NuGet.Protocol.Plugins;

namespace CredentialManager.CredentialProvider.RequestHandlers;

internal class SetLogLevelRequestHandler(ILogger<SetLogLevelRequestHandler> logger, IHostApplicationLifetime hostApplicationLifetime)
    : RequestHandlerBase<SetLogLevelRequest, SetLogLevelResponse>(logger, hostApplicationLifetime)
{
    private static readonly SetLogLevelResponse SuccessResponse = new(MessageResponseCode.Success);

    public override MessageMethod Method => MessageMethod.SetLogLevel;

    protected override Task<SetLogLevelResponse?> HandleRequestAsync(SetLogLevelRequest? request)
    {
        return Task.FromResult<SetLogLevelResponse?>(SuccessResponse);
    }
}
