using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;

namespace FluentFunctions.Functions.Configuration;

public static class AzureCredentials
{
    /// <summary>
    /// In Azure, the user-assigned managed identity named by <c>AZURE_CLIENT_ID</c>. Locally, the
    /// developer's own sign-in (Visual Studio, Azure CLI, azd). The app never holds a secret.
    /// </summary>
    public static TokenCredential Create(IConfiguration configuration) =>
        configuration["AZURE_CLIENT_ID"] is { Length: > 0 } clientId
            ? new ManagedIdentityCredential(ManagedIdentityId.FromUserAssignedClientId(clientId))
            : new DefaultAzureCredential();
}
