using System;
using AzureUtils.AzureBlobProxy.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddUserSecrets<Program>(true);
builder.ConfigureApplication();

builder.Services
    .AddMvc()
    .AddRazorRuntimeCompilation();

var connection = builder.Configuration.GetConnectionString("Blobs");

if (string.IsNullOrWhiteSpace(connection))
    throw new InvalidOperationException("Empty blob connection string provided");

builder.Services
    .AddAzureClients(azureClientFactoryBuilder =>
        azureClientFactoryBuilder.AddBlobServiceClient(connection));

builder.Services
    .AddHealthChecks()
    .AddAzureBlobStorage(connection);

var app = builder.Build();

app
    .UseStatusCodePages()
    .UseHealthChecks("/health")
    .UseSwagger()
    .UseSwaggerUI()
    .UseForwardedHeaders();

app.AddOidcAuthentication();
app.MapControllers();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

await app.RunAsync();
