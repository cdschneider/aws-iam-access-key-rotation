using AccessKeyActions.Configuration;
using AccessKeyActions.Models;
using AccessKeyActions.Repositories;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.Lambda.Annotations;
using Amazon.Lambda.Core;
using Microsoft.AspNetCore.Http;
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
    private readonly IAmazonIdentityManagementService _iamService;
    private readonly ILogger<Function> _logger;
    
    public Function(IFunctionConfiguration configuration, IAccessKeyRepository accessKeyRepository, 
        IAmazonIdentityManagementService iamService, ILogger<Function> logger)
    {
        _configuration = configuration;
        _accessKeyRepository = accessKeyRepository;
        _iamService = iamService;
        _logger = logger;
    }

    [LambdaFunction]
    public async Task<IList<AccessKeyAction>> FunctionHandler(IList<AccessKey> keys, ILambdaContext context)
    {
        if (keys == null) throw new ArgumentNullException(nameof(keys));
        if (keys.Count == 0)
            return new List<AccessKeyAction>();

        var now = DateTime.UtcNow;
        var rotationDate = now - _configuration.AccessKeyRotationWindow();
        var installationWindow = _configuration.AccessKeyInstallationWindow();
        var recoveryWindow = _configuration.AccessKeyRecoveryWindow();

        var result = new List<AccessKeyAction>();

        foreach (var key in keys.Where(k => k.Status == StatusType.Inactive))
        {
            if (key.CreateDate <= (rotationDate - (installationWindow + recoveryWindow)))
            {
                _logger.LogInformation("AccessKey {accessKey} is being marked for deletion", key.AccessKeyId);
                result.Add(new AccessKeyAction { AccessKeyId = key.AccessKeyId, Action = ActionType.Delete });
            }
        }

        foreach (var key in keys.Where(k => k.Status == StatusType.Active))
        {
            var keyDetails = await _accessKeyRepository.GetByIdAsync(key.AccessKeyId);
            
            if (key.CreateDate <= rotationDate && keyDetails?.RotationDate is null)
            {
                if (keys.Count == 2 && !result.Any(x => x.Action == ActionType.Delete))
                {
                    var otherKey = keys.First(k => k != key);
                    var otherKeyDetails = await _accessKeyRepository.GetByIdAsync(otherKey.AccessKeyId);
                    
                    if (otherKey.Status == StatusType.Active && 
                        (otherKey.CreateDate < rotationDate && !otherKeyDetails.RotationDate.HasValue))
                    {
                        AccessKey keyToDelete, keyToRotate;
                        _logger.LogWarning("Conflict detected: multiple Active, expired access keys");
                        
                        var (currLastUsed, otherLastUsed) = (
                            await _iamService.GetAccessKeyLastUsedAsync(new GetAccessKeyLastUsedRequest { AccessKeyId = key.AccessKeyId }),
                            await _iamService.GetAccessKeyLastUsedAsync(new GetAccessKeyLastUsedRequest { AccessKeyId = otherKey.AccessKeyId }));
                        
                        switch (currLastUsed, otherLastUsed)
                        {
                            // least recently-used key should be deleted
                            case (not null, not null):
                                keyToDelete = (currLastUsed.AccessKeyLastUsed.LastUsedDate >= otherLastUsed.AccessKeyLastUsed.LastUsedDate) ? otherKey : key;
                                keyToRotate = (currLastUsed.AccessKeyLastUsed.LastUsedDate >= otherLastUsed.AccessKeyLastUsed.LastUsedDate) ? key : otherKey;

                                break;
                            // least recently-created key should be deleted 
                            case (null, null):
                                keyToDelete = (key.CreateDate >= otherKey.CreateDate) ? otherKey : key;
                                keyToRotate = (key.CreateDate >= otherKey.CreateDate) ? key : otherKey;
                                
                                break;
                            // key that has not been used should be deleted
                            case (null, _):
                                keyToDelete = key;
                                keyToRotate = otherKey;
                                
                                break;
                            case (_, null) :
                                keyToDelete = otherKey;
                                keyToRotate = key;

                                break;
                        }
                        
                        _logger.LogInformation("AccessKey {accessKey} is being marked for deletion", keyToDelete.AccessKeyId);
                        result.Add(new AccessKeyAction { AccessKeyId = keyToDelete.AccessKeyId, Action = ActionType.Delete });
                        
                        _logger.LogInformation("AccessKey {accessKey} is being marked for rotation", keyToRotate.AccessKeyId);
                        result.Add(new AccessKeyAction { AccessKeyId = keyToRotate.AccessKeyId, Action = ActionType.Rotate });
                    }
                    else if (otherKey.Status == StatusType.Inactive)
                    {
                        _logger.LogWarning("Conflict detected: Active expired key requiring early-deletion of Inactive access key");

                        _logger.LogInformation("AccessKey {accessKey} is being marked for deletion", otherKey.AccessKeyId);
                        result.Add(new AccessKeyAction { AccessKeyId = otherKey.AccessKeyId, Action = ActionType.Delete });
                        
                        _logger.LogInformation("AccessKey {accessKey} is being marked for rotation", key.AccessKeyId);
                        result.Add(new AccessKeyAction { AccessKeyId = key.AccessKeyId, Action = ActionType.Rotate });
                    }
                }
                else
                {
                    _logger.LogInformation("AccessKey {accessKey} is being marked for rotation", key.AccessKeyId);
                    result.Add(new AccessKeyAction { AccessKeyId = key.AccessKeyId, Action = ActionType.Rotate });
                }
            }
            else if (key.CreateDate <= (rotationDate - installationWindow) && keyDetails?.DeactivationDate is null)
            {
                _logger.LogInformation("AccessKey {accessKey} is being marked for deactivation", key.AccessKeyId);
                result.Add(new AccessKeyAction { AccessKeyId = key.AccessKeyId, Action = ActionType.Deactivate });
            }
        }

        return result;
    }
}
