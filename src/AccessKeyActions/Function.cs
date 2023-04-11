using AccessKeyActions.Configuration;
using AccessKeyActions.Models;
using AccessKeyActions.Repositories;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.Lambda.Annotations;
using Amazon.Lambda.Core;
using Microsoft.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
//[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
[assembly: LambdaSerializer(typeof(AccessKeyActions.Serialization.CustomLambdaSerializer))]

namespace AccessKeyActions;

public class Function
{
    private readonly IFunctionConfiguration _configuration;
    private readonly IAccessKeyRepository _accessKeyRepository;
    private readonly ILogger<Function> _logger;
    
    public Function(IFunctionConfiguration configuration, IAccessKeyRepository accessKeyRepository, ILogger<Function> logger)
    {
        _configuration = configuration;
        _accessKeyRepository = accessKeyRepository;
        _logger = logger;

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Initializing new instance of Function()");
        }
    }
    
    [LambdaFunction]
    public async Task<IList<AccessKeyAction>> FunctionHandler(IList<AccessKey> keys, ILambdaContext context)
    {
        if (keys == null) throw new ArgumentNullException(nameof(keys));
        if (keys.Count == 0)
            return new List<AccessKeyAction>();

        var now = DateTime.UtcNow;
        var rotationCutoff = now - _configuration.AccessKeyRotationWindow();
        var installationWindow = _configuration.AccessKeyInstallationWindow();
        var recoveryWindow = _configuration.AccessKeyRecoveryWindow();
        
        var result = new List<AccessKeyAction>();

        foreach (var key in keys.Where(k => k.Status == StatusType.Inactive))
        {
            if (key.CreateDate < rotationCutoff - (installationWindow + recoveryWindow))
            {
                _logger.LogInformation("AccessKey {accessKey} is being marked for deletion", key.AccessKeyId);
                result.Add(new AccessKeyAction { AccessKeyId = key.AccessKeyId, Action = ActionType.Delete });
            }
        }
        
        foreach (var key in keys.Where(k => k.Status == StatusType.Active))
        {
            var keyDetails = await _accessKeyRepository.GetByIdAsync(key.AccessKeyId);
            
            if (key.CreateDate < rotationCutoff - installationWindow)
            {
                _logger.LogInformation("AccessKey {accessKey} is being marked for deactivation", key.AccessKeyId);
                result.Add(new AccessKeyAction { AccessKeyId = key.AccessKeyId, Action = ActionType.Deactivate });
            } 
            else if (key.CreateDate < rotationCutoff && !keyDetails.RotationDate.HasValue)
            {
                _logger.LogInformation("AccessKey {accessKey} is being marked for rotation", key.AccessKeyId);
                result.Add(new AccessKeyAction { AccessKeyId = key.AccessKeyId, Action = ActionType.Rotate });
            } 
        }
        
        return result;
    }
}
