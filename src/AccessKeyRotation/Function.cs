using System.Text.Json;
using AccessKeyRotation.Extensions;
using AccessKeyRotation.Models;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
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
    private readonly ILogger<Function> _logger;

    private static readonly string SecretNameFormat = "iam/{0}/accesskey";
    private static readonly string PolicyDocumentFormat = @"
{
    ""Version"": ""2012-10-17"",
    ""Statement"": [
        {
            ""Effect"": ""Allow"",
            ""Principal"": {
                ""AWS"": ""arn:aws:iam::{0}:user/{1}""
            },
            ""Action"": ""secretsmanager:GetSecretValue"",
            ""Resource"": ""*""
        }
    ]
}";

    public Function(IAmazonSecretsManager secretsManager, IAmazonIdentityManagementService iamService,
        ILogger<Function> logger)
    {
        _secretsManager = secretsManager;
        _iamService = iamService;
        _logger = logger;

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Initializing a new instance of Function()");
        }
    }
    
    public async Task FunctionHandler(AccessKeyRotationRequest input, ILambdaContext context)
    {
        var secretName = string.Format(SecretNameFormat, input.UserName);
        
        _logger.LogDebug("Executing CreateAccessKey for {username}", input.UserName);
        var newAccessKey = await _iamService.CreateAccessKeyAsync(
            new CreateAccessKeyRequest { UserName = input.UserName });
        var secretString = JsonSerializer.Serialize(newAccessKey.AccessKey);

        try
        {
            _logger.LogDebug("Executing DescribeSecret for {secretName}", secretName);
            var currentSecret = await _secretsManager.DescribeSecretAsync(
                new DescribeSecretRequest { SecretId = secretName });

            _logger.LogDebug("Executing UpdateSecret on {secretName} with AccessKey details", currentSecret.Name);
            await _secretsManager.UpdateSecretAsync(new UpdateSecretRequest { SecretId = currentSecret.Name, SecretString = secretString });
        }
        catch (ResourceNotFoundException ex)
        {
            _logger.LogWarning("Secret with name {name} does not exist", secretName);
            var secretReq = new CreateSecretRequest { Name = string.Format(SecretNameFormat, input.UserName), SecretString = secretString };
            var createdSecret = await _secretsManager.CreateSecretAsync(secretReq);
            
            _logger.LogDebug("Successfully created Secret {secretName}", secretName);
        }

        _logger.LogInformation("AccessKey has been generated for {username} and stored as {secretName}", input.UserName, secretName);
        
        var functionArn = context.FunctionArn();
        var secretPolicy = string.Format(PolicyDocumentFormat, functionArn.AccountId, input.UserName);
        
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            var policySerialized = JsonSerializer.Serialize(secretPolicy);
            _logger.LogDebug("Executing PutResourcePolicy on {secretName} with the following policy: {policy}", secretName, policySerialized);    
        }
        
        await _secretsManager.PutResourcePolicyAsync(new PutResourcePolicyRequest
        {
            SecretId = secretName, 
            ResourcePolicy = secretPolicy,
            BlockPublicPolicy = true
        });
    }
}
