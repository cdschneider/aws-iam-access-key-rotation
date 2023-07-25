using AccessKeyRotation.Services;
using Amazon.Lambda.TestUtilities;
using Xunit;

namespace AccessKeyRotation.Tests.Services;

public class LambdaFunctionArnParserTest
{
    private readonly LambdaFunctionArnParser _classUnderTest;
    public LambdaFunctionArnParserTest()
    {
        _classUnderTest = new LambdaFunctionArnParser();
    }
    
    [Theory]
    [InlineData("arn:aws:lambda:us-east-1:123456789012:function:helloworld:42")]
    [InlineData("arn:aws:lambda:us-east-1:123456789012:function:helloworld")]
    [InlineData("arn:aws:lambda:us-east-1:123456789012:function:helloworld:$LATEST")]
    [InlineData("arn:aws:lambda:us-east-1:123456789012:function:helloworld:CUSTOM-alias")]
    public void FunctionArn_FromLambdaContext_WhenGivenValidLambdaFunctionArnInput(string functionArn)
    {
        var lambdaCtx = new TestLambdaContext { InvokedFunctionArn = functionArn };
        var result = _classUnderTest.Parse(lambdaCtx.InvokedFunctionArn);
        
        Assert.NotNull(result.AccountId);
    }
}