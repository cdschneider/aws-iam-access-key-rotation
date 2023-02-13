using Amazon.IdentityManagement;
using Amazon.SecretsManager;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AccessKeyRotation;

[Amazon.Lambda.Annotations.LambdaStartup]
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();
        
        // AWS Services
        services.AddAWSService<IAmazonSecretsManager>();
        services.AddAWSService<IAmazonIdentityManagementService>();
        
        // Logging
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddConfiguration(config.GetSection("Logging"));
            builder.AddJsonConsole();
        });
    }
}