using System;
using System.IO;
using System.Text.RegularExpressions;
using AzureUtils.AzureBlobProxy.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace AzureUtils.AzureBlobProxy.Extensions;

internal static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder ConfigureApplication(this WebApplicationBuilder builder)
    {
        builder
            .AddOidcAuthentication()
            .AddContainerFilter()
            .AddPersistentDataProtection();

        builder.Services
            .AddEndpointsApiExplorer()
            .AddSwaggerGen()
            .AddSingleton<IActionResultExecutor<BlobStorageResult>, BlobStorageResultExecutor>()
            .AddTransient<IContainerValidator, ContainerValidator>()
            .Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedProto;
                // Only loopback proxies are allowed by default.
                // Clear that restriction because forwarders are enabled by explicit 
                // configuration.
                options.KnownNetworks.Clear();
                options.KnownProxies.Clear();
            });

        return builder;
    }

    private static void AddPersistentDataProtection(this WebApplicationBuilder builder)
    {
        var dataProtectionLocation = Path.Combine(AppContext.BaseDirectory, ".dataProtection");
        var dataProtectionDirectory = new DirectoryInfo(dataProtectionLocation);

        builder.Services.AddDataProtection()
            .PersistKeysToFileSystem(dataProtectionDirectory)
            .SetApplicationName(typeof(Program).Assembly.GetName().FullName)
            .UseCryptographicAlgorithms(new AuthenticatedEncryptorConfiguration
            {
                EncryptionAlgorithm = EncryptionAlgorithm.AES_256_CBC,
                ValidationAlgorithm = ValidationAlgorithm.HMACSHA256
            });
    }

    private static WebApplicationBuilder AddContainerFilter(this WebApplicationBuilder builder)
    {
        // Will allow only containers, matched with this regex
        var containerRegex = builder.Configuration.GetValue<string>("ContainerRegex");

        if (string.IsNullOrWhiteSpace(containerRegex))
            throw new InvalidOperationException("Container filter is not configured");

        Regex regex;
        try
        {
            const RegexOptions options = RegexOptions.IgnorePatternWhitespace
                                         | RegexOptions.IgnoreCase
                                         | RegexOptions.Compiled;

            regex = new Regex(containerRegex, options);
        }
        catch (Exception)
        {
            Console.WriteLine("ContainerRegex: Invalid expression.");
            throw;
        }

        builder.Services.AddSingleton<IOptions<ContainerFilter>>(
            new OptionsWrapper<ContainerFilter>(new ContainerFilter(regex)));

        return builder;
    }

    private static WebApplicationBuilder AddOidcAuthentication(this WebApplicationBuilder builder)
    {
        // Get authority server
        var authority = builder.Configuration.GetValue<string>("AuthorityServer");

        if (string.IsNullOrWhiteSpace(authority))
        {
            Console.WriteLine("No authentication is enabled.");
            return builder;
        }

        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie()
            .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.ResponseMode = OpenIdConnectResponseMode.FormPost;
                options.SaveTokens = true;
                options.UsePkce = true;
                options.Authority = authority;
                options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
                options.ClientId = "azure-blob-proxy";
                options.CallbackPath = "/signin-oidc";
            });

        return builder;
    }
}
