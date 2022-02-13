using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace AzureUtils.AzureBlobProxy.Extensions;

internal static class WebApplicationExtensions
{
    public static IApplicationBuilder AddOidcAuthentication(this WebApplication app)
    {
        // Get authority server
        var authority = app.Configuration.GetValue<string>("AuthorityServer");

        if (!string.IsNullOrWhiteSpace(authority))
            return app
                .UseAuthentication()
                .UseAuthorization();

        app.UseAuthorization();
        app
            .MapControllers()
            .WithMetadata(new AllowAnonymousAttribute());

        return app;
    }
}