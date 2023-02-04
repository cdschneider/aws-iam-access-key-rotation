using AccessKeyRotation.Models;
using AccessKeyRotation.Tests.Extensions;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using Amazon.Runtime;
using Moq.AutoMock;
using Xunit;

namespace AccessKeyRotation.Tests;

public class FunctionTest
{
    private readonly Function _classUnderTest;
    
    public FunctionTest()
    {
        var mocker = new AutoMocker();
        _classUnderTest = mocker.CreateInstance<Function>();
    }

    [Fact]
    public Task TestFunctionHandler_WhenInputIsNull_ThenArgumentNullExceptionIsThrown()
        => Assert.ThrowsAsync<ArgumentNullException>(() =>
            _classUnderTest.FunctionHandler(null!, GivenTestLambdaContext()));

    [Theory]
    [MemberData(nameof(InvalidFunctionHandlerInputs))]
    public Task TestFunctionHandler_WhenInputIncludesNullFields_ThenArgumentExceptionIsThrown(AccessKeyRotationRequest input)
        => Assert.ThrowsAsync<ArgumentException>(() => 
            _classUnderTest.FunctionHandler(input, GivenTestLambdaContext()));

    private static ILambdaContext GivenTestLambdaContext() => new TestLambdaContext().WithFunctionArn();
    
    public static IEnumerable<object[]> InvalidFunctionHandlerInputs =>
        new List<object[]>
        {
            new object[] { new AccessKeyRotationRequest { UserName = "only_a_username" } },
            new object[] { new AccessKeyRotationRequest { AccessKeyId = "only_an_access_key" } },
        };
}
