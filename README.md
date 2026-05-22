# CredentialManager.CredentialProvider

A NuGet credential provider plugin that supplies credentials from **Windows Credential Manager** for self-hosted Azure DevOps (TFS) servers.

## Why this exists

### The problem

When restoring NuGet packages against a self-hosted Azure DevOps / TFS server, NuGet tries to authenticate using the standard credential provider ecosystem. The official solution is [Microsoft's Azure Artifacts Credential Provider](https://github.com/microsoft/artifacts-credprovider) (`CredentialProvider.Microsoft`), which supports interactive device-flow login, Azure CLI tokens, and Windows Integrated Authentication (NTLM/Negotiate).

**Windows Integrated Authentication fails silently inside a Hyper-V VM** even when the host machine is domain-joined. The guest VM has no Kerberos ticket and cannot pass-through the host's domain identity, so every request returns a `401 Unauthorized`. `CredentialProvider.Microsoft` interprets this as "no credentials available" and either prompts interactively (which doesn't work in CI/automated restore) or gives up entirely.

The real credentials — a domain username and password that *do* work with the TFS server — are stored manually in **Windows Credential Manager** on the VM. `CredentialProvider.Microsoft` does not read from Credential Manager; it only tries the current Windows identity, Azure CLI, and interactive browser flows.

### The solution

This plugin reads credentials directly from Windows Credential Manager and hands them to NuGet as `Negotiate` credentials.

## How it works

1. NuGet receives a `401` from the package source and invokes all registered credential provider plugins.
2. This plugin's `WindowsCredentialProvider` is called with the request URI.
3. It searches Credential Manager for a matching entry by trying three target name formats in order of specificity:
   - Full path — `https://tfs.corp.local/tfs/DefaultCollection`
   - Authority — `https://tfs.corp.local`
   - Host — `tfs.corp.local`
4. Both **Generic** and **DomainPassword** credential types are checked for each candidate.
5. On a match, the stored username and password are returned to NuGet, which retries the request with those credentials.

## Setup

### 1. Store your credentials in Windows Credential Manager

Open **Credential Manager** → **Windows Credentials** → **Add a Windows credential** (or **Add a generic credential**):

| Field | Value |
|---|---|
| Internet or network address | Your TFS host, e.g. `tfs.corp.local` or the full URL |
| User name | `DOMAIN\username` or `username@corp.local` |
| Password | Your domain password |

The plugin tries the full URL first, then falls back to shorter forms, so storing just the hostname is usually sufficient.

### 2. Install the plugin as a .NET global tool

```shell
dotnet tool install --global CredentialManager.CredentialProvider.NuGet.Tool
```

Or install from a local package:

```shell
dotnet pack
dotnet tool install --global --add-source ./bin/Release CredentialManager.CredentialProvider.NuGet.Tool
```

NuGet automatically discovers .NET tool credential providers whose command name begins with `nuget-plugin-`. The tool command is `nuget-plugin-credential-manager-credential-provider`, so it will be picked up automatically.

### 3. Verify

Run a package restore and confirm it succeeds without interactive prompts:

```shell
dotnet restore --verbosity detailed
```

## Environment variables

| Variable | Effect |
|---|---|
| `CREDENTIALMANAGER_CREDENTIALPROVIDER_LOG_PATH` | Path to a log file. When set, debug-level logs are written there. |
| `CREDENTIALMANAGER_CREDENTIALPROVIDER_DEBUG` | Set to `1` to launch a debugger on startup. |

## Requirements

- Windows (XP SP2 / Server 2003 or later — any version that has Credential Manager)
- .NET 10 runtime
- NuGet 4.8+ (plugin protocol v2)
