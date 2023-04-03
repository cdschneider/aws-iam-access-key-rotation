using System.Text.RegularExpressions;
using Amazon.Lambda.Core;
using AccessKeyRotation.Models;

namespace AccessKeyRotation.Extensions;

public static class LambdaContextExtensions
{
    private static readonly string LambdaArnPattern =
        @"arn:(aws[a-zA-Z-]*):lambda:([a-z]{2}(-gov)?-[a-z]+-\d{1}):(\d{12}):function:([a-zA-Z0-9-_]+)(:(\$LATEST|[a-zA-Z0-9-_]+))?";
    
    public static LambdaFunctionArn FunctionArn(this ILambdaContext lambdaContext)
    {
        if (lambdaContext == null) throw new ArgumentNullException(nameof(lambdaContext));
        
        var regex = new Regex(LambdaArnPattern);
        var match = regex.Match(lambdaContext.InvokedFunctionArn);

        if (match.Success)
        {
            return new LambdaFunctionArn { AccountId = match.Groups[4].ToString() };
        }

        throw new ArgumentException("", nameof(lambdaContext.InvokedFunctionArn));
    } 
}