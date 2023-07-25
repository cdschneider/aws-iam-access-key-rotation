using AccessKeyRotation.Services;
using Amazon.IdentityManagement;
using Amazon.SecretsManager;
using Amazon.XRay.Recorder.Handlers.AwsSdk;
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
        
        // Core Services
        services.AddSingleton<IConfiguration>(config);
        services.AddTransient<ILambdaFunctionArnParser, LambdaFunctionArnParser>();
        
        // Logging
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddConfiguration(config.GetSection("Logging"));
            builder.AddJsonConsole();
        });
        
        // AWS Services
        AWSSDKHandler.RegisterXRayForAllServices();
        services.AddAWSService<IAmazonIdentityManagementService>();
        services.AddAWSService<IAmazonSecretsManager>();
    }
}