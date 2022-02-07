using System;
using AzureBlobProxy;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddMvc().AddRazorRuntimeCompilation();
builder.Services
    .AddEndpointsApiExplorer()
    .AddSwaggerGen()
    .AddSingleton<IActionResultExecutor<BlobStorageResult>, BlobStorageResultExecutor>();

var connection = builder.Configuration.GetConnectionString("Blobs");

if (string.IsNullOrWhiteSpace(connection))
    throw new InvalidOperationException("Empty blob connection string provided");

builder.Services
    .AddAzureClients(azureClientFactoryBuilder =>
        azureClientFactoryBuilder.AddBlobServiceClient(connection));

var app = builder.Build();

app.UseSwagger()
    .UseSwaggerUI()
    .UseHttpsRedirection()
    .UseAuthorization();

app.MapControllers();

app.Run();