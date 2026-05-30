using System.Diagnostics;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using NuGet.Protocol.Plugins;

namespace CredentialManager.CredentialProvider.RequestHandlers;

internal abstract class RequestHandlerBase<TRequest, TResponse> : IMessageRequestHandler
    where TResponse : class
{
    private readonly IHostApplicationLifetime _hostApplicationLifetime;

    protected RequestHandlerBase(ILogger logger, IHostApplicationLifetime hostApplicationLifetime)
    {
        Logger = logger;
        _hostApplicationLifetime = hostApplicationLifetime;
    }

    public abstract MessageMethod Method { get; }

    public virtual CancellationToken CancellationToken => _hostApplicationLifetime.ApplicationStopping;

    public IConnection? Connection { get; private set; }

    protected ILogger Logger { get; }


    public async Task HandleResponseAsync(IConnection connection, Message message, IResponseHandler responseHandler, CancellationToken cancellationToken)
    {
        var timer = Stopwatch.StartNew();

        Connection = connection;

        var request = MessageUtilities.DeserializePayload<TRequest>(message);

        try
        {
            TResponse? response = null;
            Logger.LogDebug("Handling '{RequestType}' '{Method}'. Time elapsed in ms: {ElapsedMilliseconds} - Payload: {Payload}",
                message.Type, message.Method, timer.ElapsedMilliseconds, message.Payload.ToString(Newtonsoft.Json.Formatting.None));
            try
            {
                using (GetProgressReporter(connection, message, cancellationToken))
                {
                    response = await HandleRequestAsync(request).ConfigureAwait(false);
                }
            }
            catch (Exception ex) when (cancellationToken.IsCancellationRequested)
            {
                Logger.LogDebug("Ignoring a cancellation exception during request handling. Exception: {InnerExceptoin}. Message: {Message}",
                    ex.InnerException, ex.Message);
            }

            Logger.LogDebug("Sending response: '{RequestType}' '{Method}'. Time elapsed in ms: {ElapsedMilliseconds}",
                message.Type, message.Method, timer.ElapsedMilliseconds);

            await responseHandler.SendResponseAsync(message, response, CancellationToken.None).ConfigureAwait(false);

            Logger.LogDebug("Time ellapsed in milliseconds after sending response '{RequestType}' '{Method}': {ElapsedMilliseconds}",
                message.Type, message.Method, timer.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            bool cancellingDuringShutdown = ex is OperationCanceledException && _hostApplicationLifetime.ApplicationStopping.IsCancellationRequested;

            if (cancellingDuringShutdown)
            {
                Logger.LogDebug("While the plugin was shutting down.");
            }

            Logger.LogDebug(ex, "Caught exception processing {Method} (RequestId: {RequestId})",
                message.Method, message.RequestId);
        }

        timer.Stop();
    }

    protected abstract Task<TResponse?> HandleRequestAsync(TRequest? request);

    protected virtual AutomaticProgressReporter? GetProgressReporter(IConnection connection, Message message, CancellationToken cancellationToken)
    {
        return null;
    }
}