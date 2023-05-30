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
}

internal class TestFunctionConfiguration : IFunctionConfiguration
{
    private readonly DateTime _expiationCutoff, _installationCutoff, _recoveryCutoff;
    
    public TestFunctionConfiguration(DateTime expirationCutoff, DateTime installationCutoff, DateTime recoveryCutoff)
    {
        _expiationCutoff = expirationCutoff;
        _installationCutoff = installationCutoff;
        _recoveryCutoff = recoveryCutoff;
    }

    public TimeSpan AccessKeyRotationWindow() => DateTime.UtcNow - _expiationCutoff;

    public TimeSpan AccessKeyInstallationWindow() => _installationCutoff - _expiationCutoff;

    public TimeSpan AccessKeyRecoveryWindow() => _recoveryCutoff - _installationCutoff;
} 