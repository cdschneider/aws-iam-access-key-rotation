using Amazon.DynamoDBv2;
using Amazon.IdentityManagement;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AccessKeyActions;

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
        
        // Custom Services
        services.AddSingleton<IFunctionConfiguration, FunctionConfiguration>();
        
        // AWS Services
        //services.AddAWSService<IAmazonIdentityManagementService>();
        
        // Logging
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddConfiguration(config.GetSection("Logging"));
            loggingBuilder.AddJsonConsole();
        });
    }
}