using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;

using Microsoft.Extensions.Logging;

using NuGet.Protocol.Plugins;

using MFW = Meziantou.Framework.Win32;

namespace CredentialManager.CredentialProvider.CredentialProviders;

internal sealed partial class WindowsCredentialProvider(ILogger<WindowsCredentialProvider> logger, IAuthUtil authUtil) : ICredentialProvider
{
    public bool IsCachable => false;

    public async Task<bool> CanProvideCredentialAsync(Uri uri)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(6, 0, 6000))
        {
            logger.LogDebug("Windows Credential Provider is not supported on this OS version.");
            return false;
        }

        var invalidHosts = new[]
        {
            ".pkgs.vsts.me",
            "pkgs.codedev.ms",
            "pkgs.codeapp.ms",
            ".pkgs.visualstudio.com",
            "pkgs.dev.azure.com",
        };

        bool isInvalidHost = invalidHosts.Any(host => host.StartsWith(".")
            ? uri.Host.EndsWith(host, StringComparison.OrdinalIgnoreCase)
            : uri.Host.Equals(host, StringComparison.OrdinalIgnoreCase));
        if (isInvalidHost)
        {
            logger.LogDebug("Matched well-known Azure DevOps Service host: {uri}.", uri.Host);
            return false;
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogDebug("URI scheme is not HTTPS: {uri}.", uri);
            return false;
        }

        var azDevOpsType = await authUtil.GetAzDevDeploymentTypeAsync(uri, CancellationToken.None).ConfigureAwait(false);
        if (azDevOpsType == AzDevDeploymentType.OnPrem)
        {
            logger.LogDebug("Detected an on premise Azure DevOps Server.");
            return true;
        }

        logger.LogDebug("{uri} is not a supported host for Windows Credential Provider.", uri);
        return false;

    }

    public void Dispose() { }

    public async Task<GetAuthenticationCredentialsResponse?> HandleRequestAsync(
        GetAuthenticationCredentialsRequest request,
        CancellationToken cancellationToken)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(6, 0, 6000))
        {
            return new GetAuthenticationCredentialsResponse(
                username: null,
                password: null,
                message: "CredentialManager.CredentialProvider is only available on Windows 6.0.6000 and later.",
                authenticationTypes: null,
                responseCode: MessageResponseCode.Error
            );
        }

        if (TryFindCredential(request.Uri, out var username, out var password))
        {
            return new GetAuthenticationCredentialsResponse(
                username: username,
                password: password,
                message: null,
                authenticationTypes: ["Negotiate"],
                responseCode: MessageResponseCode.Success
            );
        }

        if (request.IsNonInteractive)
        {
            return new GetAuthenticationCredentialsResponse(
                username: null,
                password: null,
                message: null,
                authenticationTypes: null,
                responseCode: MessageResponseCode.NotFound
            );
        }

        var credential = await PromptForCredentialsAsync(request.Uri, cancellationToken).ConfigureAwait(false);

        if (credential == null)
        {
            logger.LogWarning("User canceled credential prompt.");
            return new GetAuthenticationCredentialsResponse(
                username: null,
                password: null,
                message: "The user canceled the credential prompt.",
                authenticationTypes: null,
                responseCode: MessageResponseCode.Error
            );
        }

        if (credential.CredentialSaved == MFW.CredentialSaveOption.Selected)
        {
            var authority = request.Uri.GetLeftPart(UriPartial.Authority);
            MFW.CredentialManager.WriteCredential(applicationName: authority,
                userName: credential.UserName,
                secret: credential.Password,
                persistence: MFW.CredentialPersistence.Enterprise);
        }

        return new GetAuthenticationCredentialsResponse(
            username: credential.UserName,
            password: credential.Password,
            message: null,
            authenticationTypes: ["Negotiate"],
            responseCode: MessageResponseCode.Success
        );
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

    [SupportedOSPlatform("windows6.0.6000")]
    private static Task<MFW.CredentialResult?> PromptForCredentialsAsync(Uri uri, CancellationToken cancellationToken)
    {
        var tcs = new TaskCompletionSource<MFW.CredentialResult?>(TaskCreationOptions.RunContinuationsAsynchronously);

        _ = Task.Run(() =>
        {
            try
            {
                var result = MFW.CredentialManager.PromptForCredentials(
                    messageText: uri.GetLeftPart(UriPartial.Authority),
                    captionText: "NuGet is requesting credentials.",
                    saveCredential: MFW.CredentialSaveOption.Selected);
                tcs.SetResult(result);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        }, cancellationToken);

        return tcs.Task;
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
