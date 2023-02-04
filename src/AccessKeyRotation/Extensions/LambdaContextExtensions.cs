using Amazon.Lambda.Core;
using AccessKeyRotation.Models;

namespace AccessKeyRotation.Extensions;

public static class LambdaContextExtensions
{
    private static readonly string LambdaArnPattern =
        @"arn:(aws[a-zA-Z-]*):lambda:([a-z]{2}(-gov)?-[a-z]+-\d{1}):(\d{12}):function:([a-zA-Z0-9-_]+)(:(\$LATEST|[a-zA-Z0-9-_]+))?";
    
    public static LambdaFunctionArn FunctionArn(this ILambdaContext lambdaContext)
    {
        return new LambdaFunctionArn(); //TODO
    } 
}