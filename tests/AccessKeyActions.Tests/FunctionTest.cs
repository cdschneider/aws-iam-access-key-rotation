using AccessKeyActions.Configuration;
using AccessKeyActions.Models;
using AccessKeyActions.Repositories;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.Lambda.TestUtilities;
using AutoFixture;
using AutoFixture.AutoMoq;
using Moq;
using Moq.AutoMock;
using Xunit;

namespace AccessKeyActions.Tests;

public class FunctionTest
{
    private readonly AutoMocker _mocker;
    private readonly IFixture _fixture;
    private readonly Function _classUnderTest;

    public FunctionTest()
    {
        _mocker = new AutoMocker();
        _fixture = new Fixture().Customize(new AutoMoqCustomization());
        _classUnderTest = _mocker.CreateInstance<Function>();
    }

    [Fact]
    public async Task TestFunctionHandler_WhenInputIsNull_ThenArgumentNullExceptionIsThrown()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _classUnderTest.FunctionHandler(null, new TestLambdaContext()));
    }
    
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

        _mocker.GetMock<IFunctionConfiguration>().Setup(x => x.AccessKeyRotationWindow())
            .Returns(TimeSpan.FromDays(30));

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
        
        _mocker.GetMock<IFunctionConfiguration>().Setup(x => x.AccessKeyRotationWindow())
            .Returns(TimeSpan.FromDays(30));

        // act
        var result = await _classUnderTest.FunctionHandler(
            new List<AccessKey> { key }, new TestLambdaContext());

        // assert & verify
        Assert.Equal(1, result.Count);
        Assert.Contains(result, k => 
            k.AccessKeyId == key.AccessKeyId && k.Action == ActionType.Rotate);
    }
    
    [Fact]
    public async Task TestFunctionHandler_WhenOneActiveExpiredPastInstallationWindowAccessKeyExists_ThenRotationActionIsPresent()
    {
        // arrange
        var key = _fixture.Create<AccessKey>();
        key.CreateDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(90));
        key.Status = StatusType.Active;
        
        _mocker.GetMock<IFunctionConfiguration>().Setup(x => x.AccessKeyRotationWindow())
            .Returns(TimeSpan.FromDays(30));
        _mocker.GetMock<IFunctionConfiguration>().Setup(x => x.AccessKeyInstallationWindow())
            .Returns(TimeSpan.FromDays(7));

        // act
        var result = await _classUnderTest.FunctionHandler(
            new List<AccessKey> { key }, new TestLambdaContext());

        // assert & verify
        Assert.Equal(1, result.Count);
        Assert.Contains(result, k => 
            k.AccessKeyId == key.AccessKeyId && k.Action == ActionType.Rotate);
    }
    
    [Fact]
    public async Task TestFunctionHandler_WhenOneActiveExpiredPastInstallationWindowAndRotatedAccessKeyExists_ThenDeactivationActionIsPresent()
    {
        // arrange
        var key = _fixture.Create<AccessKey>();
        key.CreateDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(90));
        key.Status = StatusType.Active;

        _mocker.GetMock<IAccessKeyRepository>().Setup(x => x.GetByIdAsync(It.IsAny<string>()))
            .ReturnsAsync(new AccessKeyEntity { RotationDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(30)) });
        _mocker.GetMock<IFunctionConfiguration>().Setup(x => x.AccessKeyRotationWindow())
            .Returns(TimeSpan.FromDays(30));
        _mocker.GetMock<IFunctionConfiguration>().Setup(x => x.AccessKeyInstallationWindow())
            .Returns(TimeSpan.FromDays(7));

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

        _mocker.GetMock<IFunctionConfiguration>().Setup(x => x.AccessKeyRotationWindow())
            .Returns(TimeSpan.FromDays(30));

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

        _mocker.GetMock<IFunctionConfiguration>().Setup(x => x.AccessKeyRotationWindow())
            .Returns(TimeSpan.FromDays(30));
        _mocker.GetMock<IFunctionConfiguration>().Setup(x => x.AccessKeyInstallationWindow())
            .Returns(TimeSpan.FromDays(7));

        // act
        var result = await _classUnderTest.FunctionHandler(
            new List<AccessKey> { key }, new TestLambdaContext());

        // assert & verify
        Assert.Empty(result);
    }
    
    [Fact]
    public async Task TestFunctionHandler_WhenOneInactiveExpiredPastInstallationWindowAccessKeyExists_ThenNoActionsPresent()
    {
        // arrange
        var key = _fixture.Create<AccessKey>();
        key.CreateDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(40));
        key.Status = StatusType.Inactive;

        _mocker.GetMock<IFunctionConfiguration>().Setup(x => x.AccessKeyRotationWindow())
            .Returns(TimeSpan.FromDays(30));
        _mocker.GetMock<IFunctionConfiguration>().Setup(x => x.AccessKeyInstallationWindow())
            .Returns(TimeSpan.FromDays(7));
        _mocker.GetMock<IFunctionConfiguration>().Setup(x => x.AccessKeyRecoveryWindow())
            .Returns(TimeSpan.FromDays(7));

        // act
        var result = await _classUnderTest.FunctionHandler(
            new List<AccessKey> { key }, new TestLambdaContext());

        // assert & verify
        Assert.Empty(result);
    }
    
    [Fact]
    public async Task TestFunctionHandler_WhenOneInactiveExpiredPastInstallationWindowAndInstalledAccessKeyExists_ThenDeleteActionIsPresent()
    {
        // arrange
        var key = _fixture.Create<AccessKey>();
        key.CreateDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(90));
        key.Status = StatusType.Inactive;

        _mocker.GetMock<IAccessKeyRepository>().Setup(x => x.GetByIdAsync(It.IsAny<string>()))
            .ReturnsAsync(new AccessKeyEntity { DeactivationDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(40)) });
        _mocker.GetMock<IFunctionConfiguration>().Setup(x => x.AccessKeyRotationWindow())
            .Returns(TimeSpan.FromDays(30));
        _mocker.GetMock<IFunctionConfiguration>().Setup(x => x.AccessKeyInstallationWindow())
            .Returns(TimeSpan.FromDays(7));
        _mocker.GetMock<IFunctionConfiguration>().Setup(x => x.AccessKeyRecoveryWindow())
            .Returns(TimeSpan.FromDays(7));

        // act
        var result = await _classUnderTest.FunctionHandler(
            new List<AccessKey> { key }, new TestLambdaContext());

        // assert & verify
        Assert.Equal(1, result.Count);
        Assert.Contains(result, k => 
            k.AccessKeyId == key.AccessKeyId && k.Action == ActionType.Delete);
    }
}