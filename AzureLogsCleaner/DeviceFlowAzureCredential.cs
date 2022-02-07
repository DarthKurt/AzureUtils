using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;

namespace AzureLogsCleaner;

internal sealed class DeviceFlowAzureCredential : TokenCredential
{
    private readonly IEnumerable<string> _scopes;
    private readonly IPublicClientApplication _application;

    public DeviceFlowAzureCredential(IOptions<AzureConfiguration> configuration, IEnumerable<string> scopes)
    {
        var configuration1 = configuration.Value ?? throw new ArgumentNullException(nameof(configuration));

        _scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));

        // Create the public client application (desktop app), with a default redirect URI
        // for these. Enable PoP
        _application = PublicClientApplicationBuilder.Create(configuration1.AppId)
            .WithAuthority(AzureCloudInstance.AzurePublic, configuration1.Tenant)
            .WithRedirectUri($"msal{configuration1.AppId}://auth")
            .WithExperimentalFeatures() // Needed for PoP
            .Build();
    }

    public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public override async ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
    {
        // Invoke device code flow so user can sign-in with a browser
        var result = await GetAuthenticationAsync(_application, _scopes);

        return new AccessToken(result.AccessToken, result.ExpiresOn);
    }

        
    private static async Task<AuthenticationResult> GetAuthenticationAsync(IPublicClientApplication application, IEnumerable<string> scopes)
    {
        var accounts = await application.GetAccountsAsync();

        // All AcquireToken* methods store the tokens in the cache, so check the cache first
        var scopesArray = scopes as string[] ?? scopes.ToArray();
        try
        {
            return await application.AcquireTokenSilent(scopesArray, accounts.FirstOrDefault()).ExecuteAsync();
        }
        catch (MsalUiRequiredException)
        {
            // No token found in the cache or AAD insists that a form interactive auth is required (e.g. the tenant admin turned on MFA)
            // If you want to provide a more complex user experience, check out ex.Classification
            return await application.AcquireTokenWithDeviceCode(scopesArray, callback => {
                    Console.WriteLine(callback.Message);
                    return Task.FromResult(0);
                })
                .ExecuteAsync();
        }
    }
}