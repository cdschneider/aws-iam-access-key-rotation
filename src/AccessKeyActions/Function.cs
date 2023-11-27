using AccessKeyActions.Models;
using AccessKeyActions.Options;
using AccessKeyActions.Repositories;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.Lambda.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(AccessKeyActions.Serialization.CustomLambdaSerializer))]

namespace AccessKeyActions;

public class Function
{
    private readonly AccessKeyActionsOptions _options;
    private readonly IAccessKeyRepository _accessKeyRepository;
    private readonly IAmazonIdentityManagementService _iamService;
    private readonly ILogger<Function> _logger;
    
    public Function() : this(Startup.ServiceProvider) {}
    
    public Function(IOptions<AccessKeyActionsOptions> options, IAccessKeyRepository accessKeyRepository, 
        IAmazonIdentityManagementService iamService, ILogger<Function> logger)
    {
        _options = options.Value;
        _accessKeyRepository = accessKeyRepository;
        _iamService = iamService;
        _logger = logger;
    }
    
    private Function(IServiceProvider serviceProvider) 
    {
        _options = serviceProvider.GetRequiredService<IOptions<AccessKeyActionsOptions>>().Value;
        _accessKeyRepository = serviceProvider.GetRequiredService<IAccessKeyRepository>();
        _iamService = serviceProvider.GetRequiredService<IAmazonIdentityManagementService>();
        _logger = serviceProvider.GetRequiredService<ILogger<Function>>();
    }
    
    public async Task<IList<AccessKeyAction>> FunctionHandler(IList<AccessKey> keys, ILambdaContext context)
    {
        if (keys == null) throw new ArgumentNullException(nameof(keys));
        if (keys.Count == 0)
            return new List<AccessKeyAction>();

        if (keys.Count > 2)
            throw new ArgumentException();

        var now = DateTime.UtcNow;
        var rotationDate = now - _options.KeyRotation;
        var installationWindow = _options.KeyInstallation;
        var recoveryWindow = _options.KeyRecovery;

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
                if (keys.Count == 2 && result.All(x => x.Action != ActionType.Delete))
                {
                    var otherKey = keys.First(k => k.AccessKeyId != key.AccessKeyId);
                    var otherKeyDetails = await _accessKeyRepository.GetByIdAsync(otherKey.AccessKeyId);
                    
                    if (otherKey.Status == StatusType.Active && 
                        (otherKey.CreateDate <= rotationDate && otherKeyDetails?.RotationDate is null))
                    {
                        AccessKey keyToDelete, keyToRotate;
                        _logger.LogWarning("Conflict detected: multiple Active, expired access keys");
                        
                        var (currLastUsed, otherLastUsed) = (
                            await _iamService.GetAccessKeyLastUsedAsync(new GetAccessKeyLastUsedRequest { AccessKeyId = key.AccessKeyId }),
                            await _iamService.GetAccessKeyLastUsedAsync(new GetAccessKeyLastUsedRequest { AccessKeyId = otherKey.AccessKeyId }));

                        // least recently-used key should be deleted
                        if (currLastUsed.AccessKeyLastUsed.LastUsedDate != default &&
                            otherLastUsed.AccessKeyLastUsed.LastUsedDate != default)
                        {
                            keyToDelete = (currLastUsed.AccessKeyLastUsed.LastUsedDate >= otherLastUsed.AccessKeyLastUsed.LastUsedDate) ? otherKey : key;
                            keyToRotate = (currLastUsed.AccessKeyLastUsed.LastUsedDate >= otherLastUsed.AccessKeyLastUsed.LastUsedDate) ? key : otherKey;
                        } 
                        // least recently-created key should be deleted
                        else if (currLastUsed.AccessKeyLastUsed.LastUsedDate == default &&
                                   otherLastUsed.AccessKeyLastUsed.LastUsedDate == default)
                        {
                            keyToDelete = (key.CreateDate >= otherKey.CreateDate) ? otherKey : key;
                            keyToRotate = (key.CreateDate >= otherKey.CreateDate) ? key : otherKey;
                        }
                        // key that has not been used should be deleted
                        else
                        {
                            keyToDelete = currLastUsed.AccessKeyLastUsed.LastUsedDate == default ? key : otherKey;
                            keyToRotate = currLastUsed.AccessKeyLastUsed.LastUsedDate == default ? otherKey : key;
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

                    break;
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
