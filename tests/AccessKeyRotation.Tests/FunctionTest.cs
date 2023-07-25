using AccessKeyRotation.Models;
using AccessKeyRotation.Services;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.Lambda.TestUtilities;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using AutoFixture;
using Moq;
using Moq.AutoMock;
using Xunit;

namespace AccessKeyRotation.Tests;

public class FunctionTest
{
    private readonly AutoMocker _mocker;
    private readonly Fixture _fixture;
    private readonly Function _classUnderTest;
    
    public FunctionTest()
    {
        _fixture = new Fixture();
        _mocker = new AutoMocker();
        _classUnderTest = _mocker.CreateInstance<Function>();
    }

    [Fact]
    public Task TestFunctionHandler_WhenInputIsNull_ThenArgumentNullExceptionIsThrown()
        => Assert.ThrowsAsync<ArgumentNullException>(() =>
            _classUnderTest.FunctionHandler(null!, new TestLambdaContext()));

    [Theory]
    [MemberData(nameof(InvalidFunctionHandlerInputs))]
    public Task TestFunctionHandler_WhenInputIncludesNullFields_ThenArgumentExceptionIsThrown(AccessKeyRotationRequest input)
        => Assert.ThrowsAsync<ArgumentException>(() => 
            _classUnderTest.FunctionHandler(input, new TestLambdaContext()));

    [Fact]
    public async Task TestFunctionHandler_WhenNoSecretAlreadyExists_ThenKeyIsCreatedAndSecretIsAdded()
    {
        // arrange
        _mocker.GetMock<IAmazonSecretsManager>()
            .Setup(x => x.DescribeSecretAsync(It.IsAny<DescribeSecretRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ResourceNotFoundException(string.Empty));
        _mocker.GetMock<IAmazonIdentityManagementService>()
            .Setup(x => x.CreateAccessKeyAsync(It.IsAny<CreateAccessKeyRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_fixture.Create<CreateAccessKeyResponse>());
        _mocker.GetMock<IAmazonSecretsManager>()
            .Setup(x => x.CreateSecretAsync(It.IsAny<CreateSecretRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_fixture.Create<CreateSecretResponse>());
        _mocker.GetMock<IAmazonSecretsManager>()
            .Setup(x => x.PutResourcePolicyAsync(It.IsAny<PutResourcePolicyRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(null as PutResourcePolicyResponse);
        _mocker.GetMock<ILambdaFunctionArnParser>()
            .Setup(x => x.Parse(It.IsAny<string>()))
            .Returns(_fixture.Create<LambdaFunctionArn>());

        // act
        var request = _fixture.Create<AccessKeyRotationRequest>();
        await _classUnderTest.FunctionHandler(request, new TestLambdaContext());
        
        // assert
        _mocker.VerifyAll();
        _mocker.GetMock<IAmazonSecretsManager>()
            .Verify(x => x.UpdateSecretAsync(It.IsAny<UpdateSecretRequest>(), It.IsAny<CancellationToken>()), 
                Times.Never);
    }
    
    [Fact]
    public async Task TestFunctionHandler_WhenSecretAlreadyExists_ThenKeyIsCreatedAndSecretIsUpdated()
    {
        // arrange
        _mocker.GetMock<IAmazonSecretsManager>()
            .Setup(x => x.DescribeSecretAsync(It.IsAny<DescribeSecretRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_fixture.Create<DescribeSecretResponse>());
        _mocker.GetMock<IAmazonIdentityManagementService>()
            .Setup(x => x.CreateAccessKeyAsync(It.IsAny<CreateAccessKeyRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_fixture.Create<CreateAccessKeyResponse>());
        _mocker.GetMock<IAmazonSecretsManager>()
            .Setup(x => x.UpdateSecretAsync(It.IsAny<UpdateSecretRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_fixture.Create<UpdateSecretResponse>());
        _mocker.GetMock<IAmazonSecretsManager>()
            .Setup(x => x.PutResourcePolicyAsync(It.IsAny<PutResourcePolicyRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(null as PutResourcePolicyResponse);
        _mocker.GetMock<ILambdaFunctionArnParser>()
            .Setup(x => x.Parse(It.IsAny<string>()))
            .Returns(_fixture.Create<LambdaFunctionArn>());

        // act
        var request = _fixture.Create<AccessKeyRotationRequest>();
        await _classUnderTest.FunctionHandler(request, new TestLambdaContext());
        
        // assert
        _mocker.VerifyAll();
        _mocker.GetMock<IAmazonSecretsManager>()
            .Verify(x => x.CreateSecretAsync(It.IsAny<CreateSecretRequest>(), It.IsAny<CancellationToken>()), 
                Times.Never);
    }

    public static IEnumerable<object[]> InvalidFunctionHandlerInputs =>
        new List<object[]>
        {
            new object[] { new AccessKeyRotationRequest { UserName = "only_a_username" } },
            new object[] { new AccessKeyRotationRequest { AccessKeyId = "only_an_access_key" } },
        };
}
