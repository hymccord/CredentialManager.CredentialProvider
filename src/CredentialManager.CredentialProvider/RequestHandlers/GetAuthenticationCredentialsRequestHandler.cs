using Microsoft.Extensions.Logging;

using NuGet.Protocol.Plugins;

namespace CredentialManager.CredentialProvider.RequestHandlers;

internal class GetAuthenticationCredentialsRequestHandler : RequestHandlerBase<GetAuthenticationCredentialsRequest, GetAuthenticationCredentialsResponse>
{
    //private readonly ICache<Uri, string> _cache;
    private readonly IReadOnlyCollection<ICredentialProvider> _credentialProviders;
    private readonly TimeSpan _progressReporterTimeSpan = TimeSpan.FromSeconds(2);

    public GetAuthenticationCredentialsRequestHandler(ILogger logger,
        IReadOnlyCollection<ICredentialProvider> credentialProviders)
        : base(logger)
    {
        _credentialProviders = credentialProviders;
        //_cache = cache;
    }

    public GetAuthenticationCredentialsRequestHandler(ILogger logger, 
        IReadOnlyCollection<ICredentialProvider> credentialProviders, 
        CancellationToken cancellationToken)
        : this(logger, credentialProviders)
    {
        //_cache = GetCache(logger, cancellationToken);
    }

    protected override async Task<GetAuthenticationCredentialsResponse?> HandleRequestAsync(GetAuthenticationCredentialsRequest? request)
    {
        if (request?.Uri is null)
        {
            return new GetAuthenticationCredentialsResponse(
                username: null,
                password: null,
                message: "Request uri cannot be null",
                authenticationTypes: null,
                responseCode: MessageResponseCode.Error
                );
        }

        foreach (var credentialProvider in _credentialProviders)
        {
            if (await credentialProvider.CanProvideCredentialAsync(request.Uri).ConfigureAwait(false) == false)
            {
                continue;
            }

            try
            {
                var response = await credentialProvider.HandleRequestAsync(request, CancellationToken).ConfigureAwait(false);
                if (response is { ResponseCode: MessageResponseCode.Success })
                {
                    return response;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Failed to acquire credentials");
                return new GetAuthenticationCredentialsResponse(
                    username: null,
                    password: null,
                    message: ex.Message,
                    authenticationTypes: null,
                    responseCode: MessageResponseCode.Error
                    );
            }
        }

        Logger.LogDebug("Unable to acquire credentials.");
        return new GetAuthenticationCredentialsResponse(
            username: null,
            password: null,
            message: null,
            authenticationTypes: null,
            responseCode: MessageResponseCode.NotFound
            );
    }

    protected override AutomaticProgressReporter? GetProgressReporter(IConnection connection, Message message, CancellationToken cancellationToken)
    {
        return AutomaticProgressReporter.Create(connection, message, _progressReporterTimeSpan, cancellationToken);
    }
}
