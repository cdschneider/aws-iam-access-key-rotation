using AccessKeyActions.Configuration;
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
        return new List<AccessKeyAction>();
    }
}
