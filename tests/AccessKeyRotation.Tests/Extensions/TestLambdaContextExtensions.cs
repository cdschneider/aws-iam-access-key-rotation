using System.Text;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;

namespace AccessKeyRotation.Tests.Extensions;

public static class TestLambdaContextExtensions
{
    private static readonly Random Random = new Random();
    
    public static ILambdaContext WithFunctionArn(this TestLambdaContext lambdaContext)
    {
        var builder = new StringBuilder("arn:aws:lambda:us-east-1");
        
        // generates an account ID 12 digits in length
        builder.AppendFormat(":{0}", Random.NextInt64(100000000000, 1000000000000)); 
        builder.Append(":function");

        // generates Lambda function name max 64 chars in length
        var funcNameLength = Random.Next(65);
        var funcName = new string(Enumerable.Range(0, funcNameLength).Select(i => (char)Random.Next(65, 91)).ToArray());
        
        builder.AppendFormat(":{0}", funcName);
        lambdaContext.InvokedFunctionArn = builder.ToString();

        return lambdaContext;
    }
}