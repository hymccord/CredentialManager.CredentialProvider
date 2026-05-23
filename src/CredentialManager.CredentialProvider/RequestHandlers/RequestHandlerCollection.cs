using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

using NuGet.Protocol.Plugins;

namespace CredentialManager.CredentialProvider.RequestHandlers;

internal class RequestHandlerCollection : ConcurrentDictionary<MessageMethod, IRequestHandler>, IRequestHandlers
{
    public void Add(MessageMethod method, IRequestHandler handler)
    {
        TryAdd(method, handler);
    }

    public void AddOrUpdate(MessageMethod method, Func<IRequestHandler> addHandlerFunc, Func<IRequestHandler, IRequestHandler> updateHandlerFunc)
    {
        AddOrUpdate(method, messageMethod => addHandlerFunc(), (messageMethod, requestHandler) => updateHandlerFunc(requestHandler));
    }

    public bool TryGet(MessageMethod method, [MaybeNullWhen(false)] out IRequestHandler handler)
    {
        return TryGetValue(method, out handler);
    }

    public bool TryRemove(MessageMethod method)
    {
        return TryRemove(method, out _);
    }
}