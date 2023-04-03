using AccessKeyRotation.Extensions;
using Amazon.Lambda.TestUtilities;
using Xunit;

namespace AccessKeyRotation.Tests.Extensions;

public class LambdaContextExtensionsTest
{
    [Theory]
    [InlineData("arn:aws:lambda:us-east-1:123456789012:function:aws-access-key-expiry-StatesDateTimeAddFunctionAOT-CJN26oHfTDwg")]
    [InlineData("arn:aws:lambda:us-east-1:123456789012:function:aws-access-key-expiry-AccessKeyRotationFunction-UHgQQHF2I9ZY")]
    [InlineData("arn:aws:lambda:us-east-1:123456789012:function:aws-access-key-expiry-AccessKeyActionsFunction-OLf3akUIeK4l")]
    public void TestFunctionArn(string invokedFunctionArn)
    {
        var context = new TestLambdaContext { InvokedFunctionArn = invokedFunctionArn };
        var functionArn = context.FunctionArn();
        
        Assert.NotNull(functionArn?.AccountId);
        Assert.NotEmpty(functionArn.AccountId);
    }
}