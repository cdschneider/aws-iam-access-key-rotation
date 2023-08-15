using AccessKeyRotation.Models;
using AccessKeyRotation.Services;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.Lambda.TestUtilities;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using AutoFixture;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Serialization;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace AccessKeyRotation.Tests;

public class FunctionTest
{
    private readonly IAmazonSecretsManager _mockAwsSecretsManager;
    private readonly IAmazonIdentityManagementService _mockAwsIamService;
    private readonly ILambdaFunctionArnParser _mockLambdaFunctionArnParser;
    
    private readonly Fixture _fixture;
    private readonly Function _classUnderTest;
    
    public FunctionTest()
    {
        _mockAwsSecretsManager = Substitute.For<IAmazonSecretsManager>();
        _mockAwsIamService = Substitute.For<IAmazonIdentityManagementService>();
        _mockLambdaFunctionArnParser = Substitute.For<ILambdaFunctionArnParser>();
        
        _fixture = new Fixture();
        _classUnderTest = new Function(
            _mockAwsSecretsManager,
            _mockAwsIamService,
            _mockLambdaFunctionArnParser,
            new NullLogger<Function>()
        );
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
        _mockAwsSecretsManager.DescribeSecretAsync(Arg.Any<DescribeSecretRequest>(), Arg.Any<CancellationToken>())
            .Throws(new ResourceNotFoundException(string.Empty));
        _mockAwsIamService.CreateAccessKeyAsync(Arg.Any<CreateAccessKeyRequest>(), Arg.Any<CancellationToken>())
            .Returns(_fixture.Create<CreateAccessKeyResponse>());
        _mockAwsSecretsManager.CreateSecretAsync(Arg.Any<CreateSecretRequest>(), Arg.Any<CancellationToken>())
            .Returns(_fixture.Create<CreateSecretResponse>());
        _mockAwsSecretsManager.PutResourcePolicyAsync(Arg.Any<PutResourcePolicyRequest>(), Arg.Any<CancellationToken>())
            .Returns(null as PutResourcePolicyResponse);
        _mockLambdaFunctionArnParser.Parse(Arg.Any<string>()).Returns(_fixture.Create<LambdaFunctionArn>());

        // act
        var request = _fixture.Create<AccessKeyRotationRequest>();
        await _classUnderTest.FunctionHandler(request, new TestLambdaContext());
        
        // assert
        await _mockAwsSecretsManager.DidNotReceiveWithAnyArgs().UpdateSecretAsync(default);
    }
    
    [Fact]
    public async Task TestFunctionHandler_WhenSecretAlreadyExists_ThenKeyIsCreatedAndSecretIsUpdated()
    {
        // arrange
        _mockAwsSecretsManager.DescribeSecretAsync(Arg.Any<DescribeSecretRequest>(), Arg.Any<CancellationToken>())
            .Returns(_fixture.Create<DescribeSecretResponse>());
        _mockAwsIamService.CreateAccessKeyAsync(Arg.Any<CreateAccessKeyRequest>(), Arg.Any<CancellationToken>())
            .Returns(_fixture.Create<CreateAccessKeyResponse>());
        _mockAwsSecretsManager.UpdateSecretAsync(Arg.Any<UpdateSecretRequest>(), Arg.Any<CancellationToken>())
            .Returns(_fixture.Create<UpdateSecretResponse>());
        _mockAwsSecretsManager.PutResourcePolicyAsync(Arg.Any<PutResourcePolicyRequest>(), Arg.Any<CancellationToken>())
            .Returns(null as PutResourcePolicyResponse);
        _mockLambdaFunctionArnParser.Parse(Arg.Any<string>()).Returns(_fixture.Create<LambdaFunctionArn>());

        // act
        var request = _fixture.Create<AccessKeyRotationRequest>();
        await _classUnderTest.FunctionHandler(request, new TestLambdaContext());
        
        // assert
        await _mockAwsSecretsManager.DidNotReceiveWithAnyArgs().CreateSecretAsync(default);
    }

    public static IEnumerable<object[]> InvalidFunctionHandlerInputs =>
        new List<object[]>
        {
            new object[] { new AccessKeyRotationRequest { UserName = "only_a_username" } },
            new object[] { new AccessKeyRotationRequest { AccessKeyId = "only_an_access_key" } },
        };
}
