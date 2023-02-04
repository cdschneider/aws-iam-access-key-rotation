using AccessKeyActions.Models;
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
    private readonly ILogger<Function> _logger;
    
    public Function(IFunctionConfiguration configuration, ILogger<Function> logger)
    {
        _configuration = configuration;
        _logger = logger;

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Initializing new instance of Function()");
        }
    }
    
    [LambdaFunction]
    public IList<AccessKeyAction> FunctionHandler(IList<AccessKey> keys, ILambdaContext context)
    {
        if (keys == null) throw new ArgumentNullException(nameof(keys));
        if (keys.Count == 0)
        {
            
            return new List<AccessKeyAction>();
        }

        var now = DateTime.UtcNow;
        var result = new List<AccessKeyAction>();

        var timeUntilRotation = _configuration.AccessKeyRotationWindow();
        var timeUntilDeactivation = timeUntilRotation + _configuration.AccessKeyInstallationWindow();
        var timeUntilDeletion = timeUntilDeactivation + _configuration.AccessKeyRecoveryWindow();
        
        if (keys.Count == 1)
        {
            var key = keys.First();
            if (key.Status == StatusType.Active)
            {
                
                
                if (key.CreateDate < now - timeUntilDeactivation)
                {
                    _logger.LogInformation("AccessKey {accessKeyId} has been active for {durationInSeconds} seconds since rotation and requires deactivation",
                        key.AccessKeyId, timeUntilRotation.Seconds);
                    result.Add(new AccessKeyAction { AccessKeyId = key.AccessKeyId, Action = ActionType.Deactivate });
                }
                else if (key.CreateDate < now - timeUntilRotation)
                {
                    _logger.LogInformation("AccessKey {accessKeyId} has been active for {durationInSeconds} seconds and requires rotation", 
                        key.AccessKeyId, _configuration.AccessKeyRotationWindow().Seconds);
                    result.Add(new AccessKeyAction { AccessKeyId = key.AccessKeyId, Action = ActionType.Rotate });
                }
            }
            else if (key.Status == StatusType.Inactive)
            {
                if (key.CreateDate < now - timeUntilDeletion)
                {
                    _logger.LogInformation("AccessKey {accessKeyId} has been inactive for {durationInSeconds} seconds since deactivation and requires permanent deletion",
                        key.AccessKeyId, _configuration.AccessKeyRecoveryWindow().Seconds);
                    result.Add(new AccessKeyAction { AccessKeyId = key.AccessKeyId, Action = ActionType.Delete });
                }
            }
        }
        
        //TODO handle two keys
        
        return result;
    }
}
