using System.Diagnostics.CodeAnalysis;
using AccessKeyActions.Options;
using AccessKeyActions.Repositories;
using Amazon.DynamoDBv2;
using Amazon.IdentityManagement;
using Amazon.XRay.Recorder.Handlers.AwsSdk;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AccessKeyActions;

[ExcludeFromCodeCoverage]
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
        
        // Core Services
        services.AddSingleton<IConfiguration>(config);
        services.Configure<AccessKeyActionsOptions>(config.GetSection(AccessKeyActionsOptions.AccessKeyActionsOptionsSection));
        services.Configure<DynamoDBOptions>(config.GetSection(DynamoDBOptions.DynamoDBOptionsSection));
        services.AddTransient<IAccessKeyRepository, AccessKeyRepository>();

        // Logging
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddConfiguration(config.GetSection("Logging"));
            loggingBuilder.AddJsonConsole();
        });

        // AWS Services
        AWSSDKHandler.RegisterXRayForAllServices();
        services.AddAWSService<IAmazonDynamoDB>();
        services.AddAWSService<IAmazonIdentityManagementService>();
    }
}
