using System.Text.Json;
using AccessKeyRotation.Models;
using AccessKeyRotation.Services;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.Lambda.Annotations;
using Amazon.Lambda.Core;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace AccessKeyRotation;

public class Function
{
    private readonly IAmazonSecretsManager _secretsManager;
    private readonly IAmazonIdentityManagementService _iamService;
    private readonly ILambdaFunctionArnParser _fnArnParser;
    private readonly ILogger<Function> _logger;

    public Function(IAmazonSecretsManager secretsManager, IAmazonIdentityManagementService iamService,
        ILambdaFunctionArnParser fnArnParser, ILogger<Function> logger)
    {
        _secretsManager = secretsManager;
        _iamService = iamService;
        _fnArnParser = fnArnParser;
        _logger = logger;
    }
    
    [LambdaFunction]
    public async Task FunctionHandler(AccessKeyRotationRequest input, ILambdaContext context)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (string.IsNullOrEmpty(input.UserName))
            throw new ArgumentException("UserName cannot be empty or null", nameof(input.UserName));
        if (string.IsNullOrEmpty(input.AccessKeyId))
            throw new ArgumentException("AccessKeyId cannot be empty or null", nameof(input.AccessKeyId));

        _logger.LogDebug("Executing CreateAccessKey for {username}", input.UserName);
        var newAccessKey = await _iamService.CreateAccessKeyAsync(
            new CreateAccessKeyRequest { UserName = input.UserName });
        
        var secretName = string.Format(Constants.SecretNameFormat, input.UserName);
        var secretString = JsonSerializer.Serialize(new { AccessKeyId = newAccessKey.AccessKey.AccessKeyId, 
            SecretAccessKey = newAccessKey.AccessKey.SecretAccessKey });

        try
        {
            _logger.LogDebug("Executing DescribeSecret for {secretName}", secretName);
            var currentSecret = await _secretsManager.DescribeSecretAsync(
                new DescribeSecretRequest { SecretId = secretName });

            _logger.LogDebug("Executing UpdateSecret on {secretName} with AccessKey details", currentSecret.Name);
            await _secretsManager.UpdateSecretAsync(new UpdateSecretRequest { SecretId = currentSecret.Name, SecretString = secretString });
        }
        catch (ResourceNotFoundException)
        {
            _logger.LogWarning("Secret with name {name} does not exist", secretName);
            var secretReq = new CreateSecretRequest { Name = secretName, SecretString = secretString };
            var createdSecret = await _secretsManager.CreateSecretAsync(secretReq);
            
            _logger.LogDebug("Successfully created Secret {secretName}", createdSecret.Name);
        }

        _logger.LogInformation("AccessKey has been generated for {username} and stored as {secretName}", input.UserName, secretName);
    }
}
