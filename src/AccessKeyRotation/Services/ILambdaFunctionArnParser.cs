using AccessKeyRotation.Models;

namespace AccessKeyRotation.Services;

public interface ILambdaFunctionArnParser
{
    LambdaFunctionArn Parse(string input);
}