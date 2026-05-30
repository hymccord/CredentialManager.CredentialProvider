using System.Net.Http.Headers;

using Microsoft.Extensions.Logging;

namespace CredentialManager.CredentialProvider;

public interface IAuthUtil
{
    Task<AzDevDeploymentType> GetAzDevDeploymentTypeAsync(Uri uri, CancellationToken cancellationToken);
}

public enum AzDevDeploymentType
{
    External,
    Hosted,
    OnPrem
}

public struct AuthorizationInfo
{
    public Uri EntraAuthorityUrl { get; set; }
    public string EntraTenantId { get; set; }
}

public class AuthUtil(ILogger<AuthUtil> logger) : IAuthUtil
{
    public const string VssResourceTenant = "X-VSS-ResourceTenant";
    public const string VssAuthorizationEndpoint = "X-VSS-AuthorizationEndpoint";
    public const string VssE2EID = "X-VSS-E2EID";

    private readonly Dictionary<Uri, HttpResponseHeaders> _cache = [];

    public async Task<AzDevDeploymentType> GetAzDevDeploymentTypeAsync(Uri uri, CancellationToken cancellationToken)
    {
        var responseHeaders = await GetResponseHeadersAsync(uri, cancellationToken).ConfigureAwait(false);

        // Hosted only allows https
        if (IsHttpsScheme(uri) && responseHeaders.Contains(VssResourceTenant) && responseHeaders.Contains(VssAuthorizationEndpoint))
        {
            return AzDevDeploymentType.Hosted;
        }

        if (responseHeaders.Contains(VssE2EID))
        {
            return AzDevDeploymentType.OnPrem;
        }

        return AzDevDeploymentType.External;
    }

    protected virtual async Task<HttpResponseHeaders> GetResponseHeadersAsync(Uri uri, CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(uri, out var headers))
        {
            return headers;
        }

        using var httpClient = new HttpClient(new HttpClientHandler
        {
            UseDefaultCredentials = false
        });

        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        
        logger.LogDebug("GET {uri}", uri);
        var response = await httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);

        _cache[uri] = response.Headers;

        return response.Headers;
    }

    private static bool IsHttpsScheme(Uri uri)
    {
        try
        {
            return uri.Scheme.Equals("https", StringComparison.InvariantCultureIgnoreCase);
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}