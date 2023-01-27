using AccessKeyRotation.Models;
using Amazon.Lambda.TestUtilities;
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

    [Theory]
    [MemberData(nameof(InvalidFunctionHandlerInputs))]
    public Task TestFunctionHandler_WhenInputIncludesNullFields_ThenArgumentExceptionIsThrown(AccessKeyRotationRequest input)
        => Assert.ThrowsAsync<ArgumentException>(() => 
            _classUnderTest.FunctionHandler(input, new TestLambdaContext()));

    public static IEnumerable<object[]> InvalidFunctionHandlerInputs =>
        new List<object[]>
        {
            new object[] { (null as AccessKeyRotationRequest)! },
            new object[] { new AccessKeyRotationRequest { UserName = "username_456" } },
            new object[] { new AccessKeyRotationRequest { AccessKeyId = "access_key123" } },
        };
}
