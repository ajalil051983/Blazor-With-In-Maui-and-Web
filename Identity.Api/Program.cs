using System.IO;
using IdentityServer4.EntityFramework.DbContexts;
using Identity.Api.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Identity.Api;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    ApplicationName = typeof(Program).Assembly.FullName,
    ContentRootPath = Directory.GetCurrentDirectory(),
    EnvironmentName = Environments.Staging,
});
var config = builder.Configuration;
var startup = new Startup(config);
startup.ConfigureServices(builder.Services);

if (config.GetValue<bool>("UseVault", false))
{
    config.AddAzureKeyVault(
        $"https://{config["Vault:Name"]}.vault.azure.net/",
        config["Vault:ClientId"],
        config["Vault:ClientSecret"]);
}
var app = builder.Build();
SeedData.EnsureSeedData(config, app.Logger);
startup.Configure(app, app.Environment);
app.Run();

