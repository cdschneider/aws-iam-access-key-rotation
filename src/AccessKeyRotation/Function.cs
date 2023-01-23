using System.Text.Json;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.Lambda.Core;
using Amazon.Runtime;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Logging;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace AccessKeyRotation;

public class Function
{
    private readonly IAmazonSecretsManager _secretsManager;
    private readonly IAmazonIdentityManagementService _iamService;

    private readonly ILogger<Function> _logger;

    /// <summary>
    /// 
    /// </summary>
    private static readonly string PolicyFormat = @"
{
    
}";
    
    public async Task FunctionHandler(AccessKeyRotationRequest input, ILambdaContext context)
    {
        var currentAccessKeys = await _iamService.ListAccessKeysAsync(
            new ListAccessKeysRequest { UserName = input.UserName });
        var newAccessKey = await _iamService.CreateAccessKeyAsync(
            new CreateAccessKeyRequest { UserName = input.UserName });
        
        var secret = new CreateSecretRequest { Name = $"iam/{input.UserName}/accesskey", 
            SecretString = JsonSerializer.Serialize(newAccessKey.AccessKey) };
        var createdSecret = await _secretsManager.CreateSecretAsync(secret);
        
        await _secretsManager.PutResourcePolicyAsync(new PutResourcePolicyRequest
        {
            SecretId = secret.Name, 
            ResourcePolicy = string.Format(PolicyFormat, input.UserName),
            BlockPublicPolicy = true
        });
    }
}
