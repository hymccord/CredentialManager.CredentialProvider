using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;

using NuGet.Protocol.Plugins;

using MFW = Meziantou.Framework.Win32;

namespace CredentialManager.CredentialProvider.CredentialProviders;

internal sealed partial class WindowsCredentialProvider : ICredentialProvider
{
    public bool IsCachable => false;

    public Task<bool> CanProvideCredentialAsync(Uri uri)
        => Task.FromResult(OperatingSystem.IsWindowsVersionAtLeast(5, 1, 2600));

    public void Dispose() { }

    public Task<GetAuthenticationCredentialsResponse?> HandleRequestAsync(
        GetAuthenticationCredentialsRequest request,
        CancellationToken cancellationToken)
    {
        GetAuthenticationCredentialsResponse? response = null;
        
        if (!OperatingSystem.IsWindowsVersionAtLeast(5, 1, 2600))
        {
            response = new GetAuthenticationCredentialsResponse(
                username: null,
                password: null,
                message: "CredentialManager.CredentialProvider is only available on Windows 5.1.2600 and later.",
                authenticationTypes: null,
                responseCode: MessageResponseCode.Error
            );
        }
        else
        {
           
            bool success = TryFindCredential(request.Uri, out var username, out var password);
            response = new GetAuthenticationCredentialsResponse(
                username: username,
                password: password,
                message: null,
                authenticationTypes: success ? ["Negotiate"] : null,
                responseCode: success ? MessageResponseCode.Success : MessageResponseCode.NotFound
            );
        }

        return Task.FromResult<GetAuthenticationCredentialsResponse?>(response);
    }

    /// <summary>
    /// Searches Windows Credential Manager for a credential matching the given URI.
    /// Tries progressively shorter target names (full URI → authority → host) and both
    /// Generic and DomainPassword credential types.
    /// </summary>
    [SupportedOSPlatform("windows5.1.2600")]
    private static bool TryFindCredential(Uri uri, [NotNullWhen(true)] out string? username, [NotNullWhen(true)] out string? password)
    {
        username = null;
        password = null;

        foreach (var target in BuildTargetCandidates(uri))
        {
            foreach (var credType in new[] { MFW.CredentialType.Generic, MFW.CredentialType.DomainPassword })
            {
                if (MFW.CredentialManager.ReadCredential(target, credType) is not MFW.Credential credential)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(credential.UserName) || string.IsNullOrEmpty(credential.Password))
                {
                    continue;
                }

                username = credential.UserName;
                password = credential.Password;

                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns candidate target strings in descending specificity so that the most specific
    /// match wins. Windows Credential Manager targets are typically the server authority or host.
    /// </summary>
    private static IEnumerable<string> BuildTargetCandidates(Uri uri)
    {
        // Full URI path (e.g. https://tfs.corp.local/tfs/DefaultCollection)
        var fullPath = uri.GetLeftPart(UriPartial.Path).TrimEnd('/');
        // Authority only (e.g. https://tfs.corp.local)
        var authority = uri.GetLeftPart(UriPartial.Authority);
        // Host only (e.g. tfs.corp.local) — common for domain/generic entries
        var host = uri.Host;

        return new[] { fullPath, authority, host }
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }
}
