using AccessKeyActions.Configuration;
using AccessKeyActions.Models;
using AccessKeyActions.Repositories;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.Lambda.TestUtilities;
using AutoFixture;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace AccessKeyActions.Tests;

public class FunctionTest
{
    private readonly IFunctionConfiguration _mockFunctionConfiguration;
    private readonly IAccessKeyRepository _mockAccessKeyRepository;
    private readonly IAmazonIdentityManagementService _mockAwsIamService;
    
    private readonly IFixture _fixture;
    private readonly Function _classUnderTest;

    public FunctionTest()
    {
        _mockFunctionConfiguration = Substitute.For<IFunctionConfiguration>();
        _mockAccessKeyRepository = Substitute.For<IAccessKeyRepository>();
        _mockAwsIamService = Substitute.For<IAmazonIdentityManagementService>();
        
        _fixture = new Fixture();
        _classUnderTest = new Function(
            _mockFunctionConfiguration, 
            _mockAccessKeyRepository, 
            _mockAwsIamService,
            new NullLogger<Function>()
        );
    }

    [Fact]
    public async Task TestFunctionHandler_WhenInputIsNull_ThenArgumentNullExceptionIsThrown() =>
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _classUnderTest.FunctionHandler(null, new TestLambdaContext()));

    [Fact]
    public async Task TestFunctionHandler_WhenNoAccessKeysExist_ThenNoActionsPresent()
    {
        // act
        var result = await _classUnderTest.FunctionHandler(
            new List<AccessKey>(), new TestLambdaContext());
        
        // assert & verify
        Assert.Empty(result);
    }

    [Fact]
    public async Task TestFunctionHandler_WhenOneActiveNonExpiredAccessKeyExists_ThenNoActionsPresent()
    {
        // arrange
        var key = _fixture.Create<AccessKey>();
        key.CreateDate = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(10));
        key.Status = StatusType.Active;

        _mockFunctionConfiguration.AccessKeyRotationWindow().Returns(TimeSpan.FromDays(30));

        // act
        var result = await _classUnderTest.FunctionHandler(
            new List<AccessKey> { key }, new TestLambdaContext());

        // assert & verify
        Assert.Empty(result);
    }
    
    [Fact]
    public async Task TestFunctionHandler_WhenOneActiveExpiredAccessKeyExists_ThenRotationActionIsPresent()
    {
        // arrange
        var key = _fixture.Create<AccessKey>();
        key.CreateDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(30));
        key.Status = StatusType.Active;
        
        _mockFunctionConfiguration.AccessKeyRotationWindow().Returns(TimeSpan.FromDays(30));

        // act
        var result = await _classUnderTest.FunctionHandler(
            new List<AccessKey> { key }, new TestLambdaContext());

        // assert & verify
        Assert.Equal(1, result.Count);
        Assert.Contains(result, k => 
            k.AccessKeyId == key.AccessKeyId && k.Action == ActionType.Rotate);
    }

    [Fact]
    public async Task TestFunctionHandler_WhenOneActiveExpiredAndRotatedAccessKeyExists_ThenDeactivationActionIsPresent()
    {
        // arrange
        var key = _fixture.Create<AccessKey>();
        key.CreateDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(90));
        key.Status = StatusType.Active;
        
        _mockFunctionConfiguration.AccessKeyRotationWindow().Returns(TimeSpan.FromDays(30));
        _mockFunctionConfiguration.AccessKeyInstallationWindow().Returns(TimeSpan.FromDays(7));
        _mockAccessKeyRepository.GetByIdAsync(Arg.Any<string>()).Returns(new AccessKeyEntity
            { RotationDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(30)) });

        // act
        var result = await _classUnderTest.FunctionHandler(
            new List<AccessKey> { key }, new TestLambdaContext());

        // assert & verify
        Assert.Equal(1, result.Count);
        Assert.Contains(result, k => 
            k.AccessKeyId == key.AccessKeyId && k.Action == ActionType.Deactivate);
    }

    [Fact]
    public async Task TestFunctionHandler_WhenOneInactiveNonExpiredAccessKeyExists_ThenNoActionsPresent()
    {
        // arrange
        var key = _fixture.Create<AccessKey>();
        key.CreateDate = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(10));
        key.Status = StatusType.Inactive;
        
        _mockFunctionConfiguration.AccessKeyRotationWindow().Returns(TimeSpan.FromDays(30));

        // act
        var result = await _classUnderTest.FunctionHandler(
            new List<AccessKey> { key }, new TestLambdaContext());

        // assert & verify
        Assert.Empty(result);
    }
    
    [Fact]
    public async Task TestFunctionHandler_WhenOneInactiveExpiredAccessKeyExists_ThenNoActionsPresent()
    {
        // arrange
        var key = _fixture.Create<AccessKey>();
        key.CreateDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(30));
        key.Status = StatusType.Inactive;

        _mockFunctionConfiguration.AccessKeyRotationWindow().Returns(TimeSpan.FromDays(30));
        _mockFunctionConfiguration.AccessKeyInstallationWindow().Returns(TimeSpan.FromDays(7));

        // act
        var result = await _classUnderTest.FunctionHandler(
            new List<AccessKey> { key }, new TestLambdaContext());

        // assert & verify
        Assert.Empty(result);
    }

    [Fact]
    public async Task TestFunctionHandler_WhenOneInactiveExpiredAndDeactivatedAccessKeyExists_ThenDeleteActionIsPresent()
    {
        // arrange
        var key = _fixture.Create<AccessKey>();
        key.CreateDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(90));
        key.Status = StatusType.Inactive;
        
        _mockFunctionConfiguration.AccessKeyRotationWindow().Returns(TimeSpan.FromDays(30));
        _mockFunctionConfiguration.AccessKeyInstallationWindow().Returns(TimeSpan.FromDays(7));
        _mockFunctionConfiguration.AccessKeyRecoveryWindow().Returns(TimeSpan.FromDays(7));
        _mockAccessKeyRepository.GetByIdAsync(Arg.Any<string>()).Returns(new AccessKeyEntity
            { DeactivationDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(40)) });

        // act
        var result = await _classUnderTest.FunctionHandler(
            new List<AccessKey> { key }, new TestLambdaContext());

        // assert & verify
        Assert.Equal(1, result.Count);
        Assert.Contains(result, k => 
            k.AccessKeyId == key.AccessKeyId && k.Action == ActionType.Delete);
    }

    [Fact]
    public async Task TestFunctionHandler_WhenTwoActiveNonExpiredAccessKeysExist_ThenNoActionsPresent()
    {
        // arrange
        var keys = _fixture.CreateMany<AccessKey>(2).ToList();
        keys[0].CreateDate = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(10));
        keys[0].Status = StatusType.Active;
        keys[1].CreateDate = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(10));
        keys[1].Status = StatusType.Active;

        _mockFunctionConfiguration.AccessKeyRotationWindow().Returns(TimeSpan.FromDays(30));

        // act
        var result = await _classUnderTest.FunctionHandler(
            keys, new TestLambdaContext());

        // assert & verify
        Assert.Empty(result);
    }
    
    [Fact]
    public async Task TestFunctionHandler_WhenTwoActiveExpiredAccessKeysExistAndNeitherHaveBeenUsed_ThenOneRotationActionAndOneDeleteActionArePresent()
    {
        // arrange
        var keys = _fixture.CreateMany<AccessKey>(2).ToList();
        keys[0].CreateDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(30));
        keys[0].Status = StatusType.Active;
        keys[1].CreateDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(30));
        keys[1].Status = StatusType.Active;
        
        _mockFunctionConfiguration.AccessKeyRotationWindow().Returns(TimeSpan.FromDays(30));
        _mockAwsIamService.GetAccessKeyLastUsedAsync(Arg.Any<GetAccessKeyLastUsedRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GetAccessKeyLastUsedResponse
            {
                AccessKeyLastUsed = new AccessKeyLastUsed { LastUsedDate = default }
            });

        // act
        var result = await _classUnderTest.FunctionHandler(
            keys, new TestLambdaContext());

        // assert & verify
        Assert.Equal(2, result.Count);
        Assert.Collection(result, 
            k => Assert.Equal(ActionType.Delete, k.Action), 
            k => Assert.Equal(ActionType.Rotate, k.Action)
        );
    }

    [Fact]
    public async Task TestFunctionHandler_WhenTwoActiveExpiredAccessKeysExistAndBothHaveBeenUsed_ThenOneRotationAndOneDeleteActionArePresent_And_LeastRecentlyUsedIsDeleted()
    {
        // arrange
        var keys = _fixture.CreateMany<AccessKey>(2).ToList();
        keys[0].CreateDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(30));
        keys[0].Status = StatusType.Active;
        keys[1].CreateDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(30));
        keys[1].Status = StatusType.Active;
        
        _mockFunctionConfiguration.AccessKeyRotationWindow().Returns(TimeSpan.FromDays(30));
        _mockAwsIamService.GetAccessKeyLastUsedAsync(Arg.Is<GetAccessKeyLastUsedRequest>(r => r.AccessKeyId == keys[0].AccessKeyId), Arg.Any<CancellationToken>())
            .Returns(new GetAccessKeyLastUsedResponse
            {
                AccessKeyLastUsed = new AccessKeyLastUsed
                    { LastUsedDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(7)) }
            });
        _mockAwsIamService.GetAccessKeyLastUsedAsync(Arg.Is<GetAccessKeyLastUsedRequest>(r => r.AccessKeyId == keys[1].AccessKeyId), Arg.Any<CancellationToken>())
            .Returns(new GetAccessKeyLastUsedResponse
            {
                AccessKeyLastUsed = new AccessKeyLastUsed
                    { LastUsedDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(3)) }
            });
        
        // act
        var result = await _classUnderTest.FunctionHandler(
            keys, new TestLambdaContext());

        // assert & verify
        Assert.Equal(2, result.Count);
        Assert.Collection(result, 
            k =>
            {
                Assert.Equal(keys[0].AccessKeyId, k.AccessKeyId);
                Assert.Equal(ActionType.Delete, k.Action);
            }, 
            k =>
            {
                Assert.Equal(keys[1].AccessKeyId, k.AccessKeyId);
                Assert.Equal(ActionType.Rotate, k.Action);
            });
    }

    [Fact]
    public async Task TestFunctionHandler_WhenTwoActiveExpiredAccessKeysExistAndOnlyOneHasBeenUsed_ThenOneRotationAndOneDeleteActionArePresent_And_UnusedKeyIsDeleted()
    {
        // arrange
        var keys = _fixture.CreateMany<AccessKey>(2).ToList();
        keys[0].CreateDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(30));
        keys[0].Status = StatusType.Active;
        keys[1].CreateDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(30));
        keys[1].Status = StatusType.Active;
        
        _mockFunctionConfiguration.AccessKeyRotationWindow().Returns(TimeSpan.FromDays(30));
        _mockAwsIamService.GetAccessKeyLastUsedAsync(Arg.Is<GetAccessKeyLastUsedRequest>(r => r.AccessKeyId == keys[0].AccessKeyId), Arg.Any<CancellationToken>())
            .Returns(new GetAccessKeyLastUsedResponse
            {
                AccessKeyLastUsed = new AccessKeyLastUsed { LastUsedDate = default }
            });
        _mockAwsIamService.GetAccessKeyLastUsedAsync(Arg.Is<GetAccessKeyLastUsedRequest>(r => r.AccessKeyId == keys[1].AccessKeyId), Arg.Any<CancellationToken>())
            .Returns(new GetAccessKeyLastUsedResponse
            {
                AccessKeyLastUsed = new AccessKeyLastUsed
                    { LastUsedDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(3)) }
            });
        
        // act
        var result = await _classUnderTest.FunctionHandler(
            keys, new TestLambdaContext());

        // assert & verify
        Assert.Equal(2, result.Count);
        Assert.Collection(result, 
            k =>
            {
                Assert.Equal(keys[0].AccessKeyId, k.AccessKeyId);
                Assert.Equal(ActionType.Delete, k.Action);
            }, 
            k =>
            {
                Assert.Equal(keys[1].AccessKeyId, k.AccessKeyId);
                Assert.Equal(ActionType.Rotate, k.Action);
            });
    }
        
    [Fact]
    public async Task TestFunctionHandler_WhenTwoActiveExpiredAndRotatedAccessKeysExist_ThenTwoDeactivationActionsArePresent()
    {
        // arrange
        var keys = _fixture.CreateMany<AccessKey>(2).ToList();
        keys[0].CreateDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(90));
        keys[0].Status = StatusType.Active;
        keys[1].CreateDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(90));
        keys[1].Status = StatusType.Active;
        
        _mockFunctionConfiguration.AccessKeyRotationWindow().Returns(TimeSpan.FromDays(30));
        _mockFunctionConfiguration.AccessKeyInstallationWindow().Returns(TimeSpan.FromDays(7));
        _mockAccessKeyRepository.GetByIdAsync(Arg.Any<string>()).Returns(new AccessKeyEntity
            { RotationDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(30)) });

        // act
        var result = await _classUnderTest.FunctionHandler(
            keys, new TestLambdaContext());

        // assert & verify
        Assert.Equal(2, result.Count);
        Assert.Contains(result, k => 
            k.AccessKeyId == keys[0].AccessKeyId && k.Action == ActionType.Deactivate);
        Assert.Contains(result, k => 
            k.AccessKeyId == keys[1].AccessKeyId && k.Action == ActionType.Deactivate);
    }
    
    //TODO: more test cases with two Active, Expired access keys (last recently-used, earliest created, etc.) 
    
    [Fact(Skip = "Still need to study this test case")]
    public async Task TestFunctionHandler_WhenOneActiveNonExpiredAndOneActiveExpiredAccessKeysExist_ThenOneRotationActionAndOneDeleteActionArePresent()
    {
        // arrange
        var keys = _fixture.CreateMany<AccessKey>(2).ToList();
        keys[0].CreateDate = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(10));
        keys[0].Status = StatusType.Active;
        keys[1].CreateDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(30));
        keys[1].Status = StatusType.Active;
        
        _mockFunctionConfiguration.AccessKeyRotationWindow().Returns(TimeSpan.FromDays(30));
        
        // act
        var result = await _classUnderTest.FunctionHandler(
            keys, new TestLambdaContext());

        // assert & verify
        Assert.Equal(2, result.Count);
        Assert.Collection(result, 
            k => Assert.Equal(ActionType.Delete, k.Action), 
            k => Assert.Equal(ActionType.Rotate, k.Action)
        );
    }
    
    [Fact]
    public async Task TestFunctionHandler_WhenTwoInactiveExpiredAccessKeysExist_ThenNoActionsPresent()
    {
        // arrange
        var keys = _fixture.CreateMany<AccessKey>(2).ToList();
        keys[0].CreateDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(30));
        keys[0].Status = StatusType.Inactive;
        keys[1].CreateDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(30));
        keys[1].Status = StatusType.Inactive;

        _mockFunctionConfiguration.AccessKeyRotationWindow().Returns(TimeSpan.FromDays(30));
        _mockFunctionConfiguration.AccessKeyInstallationWindow().Returns(TimeSpan.FromDays(7));

        // act
        var result = await _classUnderTest.FunctionHandler(
            keys, new TestLambdaContext());

        // assert & verify
        Assert.Empty(result);
    }
    
    [Fact]
    public async Task TestFunctionHandler_WhenTwoInactiveAndDeactivatedExpiredAccessKeysExist_ThenTwoDeletionActionsPresent()
    {
        // arrange
        var keys = _fixture.CreateMany<AccessKey>(2).ToList();
        keys[0].CreateDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(90));
        keys[0].Status = StatusType.Inactive;
        keys[1].CreateDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(90));
        keys[1].Status = StatusType.Inactive;
        
        _mockFunctionConfiguration.AccessKeyRotationWindow().Returns(TimeSpan.FromDays(30));
        _mockFunctionConfiguration.AccessKeyInstallationWindow().Returns(TimeSpan.FromDays(7));
        _mockFunctionConfiguration.AccessKeyRecoveryWindow().Returns(TimeSpan.FromDays(7));
        _mockAccessKeyRepository.GetByIdAsync(Arg.Any<string>()).Returns(new AccessKeyEntity
            { DeactivationDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(40)) });

        // act
        var result = await _classUnderTest.FunctionHandler(
            keys, new TestLambdaContext());

        // assert & verify
        Assert.Equal(2, result.Count);
        Assert.Contains(result, k => 
            k.AccessKeyId == keys[0].AccessKeyId && k.Action == ActionType.Delete);
        Assert.Contains(result, k => 
            k.AccessKeyId == keys[1].AccessKeyId && k.Action == ActionType.Delete);
    }

    [Fact]
    public async Task TestFunctionHandler_WhenOneActiveNonExpiredAccessKeyAndOneActiveExpiredAndRotatedAccessKeyExists_ThenOneDeactivationActionIsPresent()
    {
        // arrange
        var keys = _fixture.CreateMany<AccessKey>(2).ToList();
        keys[0].CreateDate = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(10));
        keys[0].Status = StatusType.Active;
        keys[1].CreateDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(90));
        keys[1].Status = StatusType.Active;
        
        _mockFunctionConfiguration.AccessKeyRotationWindow().Returns(TimeSpan.FromDays(30));
        _mockFunctionConfiguration.AccessKeyInstallationWindow().Returns(TimeSpan.FromDays(7));
        _mockAccessKeyRepository.GetByIdAsync(keys[1].AccessKeyId).Returns(new AccessKeyEntity
            { RotationDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(30)) });

        // act
        var result = await _classUnderTest.FunctionHandler(
            keys, new TestLambdaContext());

        // assert & verify
        Assert.Equal(1, result.Count);
        Assert.Contains(result, k => 
            k.AccessKeyId == keys[1].AccessKeyId && k.Action == ActionType.Deactivate);
    }
    
    [Fact]
    public async Task TestFunctionHandler_WhenOneActiveNonExpiredAccessKeyAndOneInactiveExpiredAndDeactivatedAccessKey_ThenOneDeleteActionIsPresent()
    {
        // arrange
        var keys = _fixture.CreateMany<AccessKey>(2).ToList();
        keys[0].CreateDate = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(10));
        keys[0].Status = StatusType.Active;
        keys[1].CreateDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(45));
        keys[1].Status = StatusType.Inactive;
        
        _mockFunctionConfiguration.AccessKeyRotationWindow().Returns(TimeSpan.FromDays(30));
        _mockFunctionConfiguration.AccessKeyInstallationWindow().Returns(TimeSpan.FromDays(7));
        _mockFunctionConfiguration.AccessKeyRecoveryWindow().Returns(TimeSpan.FromDays(7));
        _mockAccessKeyRepository.GetByIdAsync(keys[1].AccessKeyId).Returns(new AccessKeyEntity
            { DeactivationDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(40)) });

        // act
        var result = await _classUnderTest.FunctionHandler(
            keys, new TestLambdaContext());

        // assert & verify
        Assert.Equal(1, result.Count);
        Assert.Contains(result, k => 
            k.AccessKeyId == keys[1].AccessKeyId && k.Action == ActionType.Delete);
    }

    [Fact]
    public async Task TestFunctionHandler_WhenOneActiveExpiredAccessKeyAndOneInactiveAccessKeyExists_ThenOneRotationActionAndOneDeleteActionArePresent()
    {
        // arrange
        var keys = _fixture.CreateMany<AccessKey>(2).ToList();
        keys[0].CreateDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(30));
        keys[0].Status = StatusType.Active;
        keys[1].CreateDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(30));
        keys[1].Status = StatusType.Inactive;

        _mockFunctionConfiguration.AccessKeyRotationWindow().Returns(TimeSpan.FromDays(30));
        _mockFunctionConfiguration.AccessKeyInstallationWindow().Returns(TimeSpan.FromDays(7));

        // act
        var result = await _classUnderTest.FunctionHandler(
            keys, new TestLambdaContext());

        // assert & verify
        Assert.Equal(2, result.Count);
        Assert.Collection(result, 
            k => Assert.Equal(ActionType.Delete, k.Action), 
            k => Assert.Equal(ActionType.Rotate, k.Action)
        );
    }
    
    [Fact]
    public async Task TestFunctionHandler_WhenOneActiveExpiredAccessKeyAndOneInactiveExpiredAndDeactivatedAccessKey_ThenOneRotationAndOneDeleteActionArePresent()
    {
        // arrange
        var keys = _fixture.CreateMany<AccessKey>(2).ToList();
        keys[0].CreateDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(30));
        keys[0].Status = StatusType.Active;
        keys[1].CreateDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(45));
        keys[1].Status = StatusType.Inactive;
        
        _mockFunctionConfiguration.AccessKeyRotationWindow().Returns(TimeSpan.FromDays(30));
        _mockFunctionConfiguration.AccessKeyInstallationWindow().Returns(TimeSpan.FromDays(7));
        _mockFunctionConfiguration.AccessKeyRecoveryWindow().Returns(TimeSpan.FromDays(7));
        _mockAccessKeyRepository.GetByIdAsync(keys[1].AccessKeyId).Returns(new AccessKeyEntity
            { DeactivationDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(40)) });

        // act
        var result = await _classUnderTest.FunctionHandler(
            keys, new TestLambdaContext());

        // assert & verify
        Assert.Equal(2, result.Count);
        Assert.Collection(result, 
            k => Assert.Equal(ActionType.Delete, k.Action), 
            k => Assert.Equal(ActionType.Rotate, k.Action)
        );
    }
    
    [Fact]
    public async Task TestFunctionHandler_WhenMoreThanTwoAccessKeysExist_ThenArgumentExceptionIsThrown()
    {
        var keys = _fixture.CreateMany<AccessKey>(3).ToList();
        await Assert.ThrowsAsync<ArgumentException>(() => 
            _classUnderTest.FunctionHandler(keys, new TestLambdaContext()));
    }
}