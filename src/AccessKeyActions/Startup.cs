using AccessKeyActions.Configuration;
using AccessKeyActions.Options;
using AccessKeyActions.Repositories;
using Amazon.DynamoDBv2;
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
        
        // Core Services
        services.AddSingleton<IConfiguration>(config);
        services.Configure<DynamoDBOptions>(config.GetSection(DynamoDBOptions.DynamoDBOptionsSection));
        
        services.AddSingleton<IFunctionConfiguration, FunctionConfiguration>();
        services.AddTransient<IAccessKeyRepository, AccessKeyRepository>();

        // Logging
        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.ClearProviders();
            loggingBuilder.AddConfiguration(config.GetSection("Logging"));
            loggingBuilder.AddJsonConsole();
        });

        // AWS Services
        services.AddAWSService<IAmazonDynamoDB>();
    }
}