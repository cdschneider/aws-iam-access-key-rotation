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

    private static readonly DateTime ExpirationCutoff = new DateTime(1999, 1, 1);
    private static readonly DateTime InstallationCutoff = new DateTime(1999, 1, 15);
    private static readonly DateTime RecoveryCutoff = new DateTime(1999, 1, 20);

    public FunctionTest()
    {
        _mocker = new AutoMocker();
        _fixture = new Fixture().Customize(new AutoMoqCustomization());
        
        _mocker.Use<IFunctionConfiguration>(new TestFunctionConfiguration(ExpirationCutoff, InstallationCutoff, RecoveryCutoff));
        _classUnderTest = _mocker.CreateInstance<Function>();
    }

    [Fact]
    public async Task TestFunctionHandler_WhenNoAccessKeysExist_ThenNoActionsPresent()
    {
        // act
        var result = await _classUnderTest.FunctionHandler(
            new List<AccessKey>(), new TestLambdaContext());
        
        // assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task TestFunctionHandler_WhenOneActiveExpiredAccessKeyExists_ThenRotationActionIsPresent()
    {
        // arrange
        var key = _fixture.Create<AccessKey>();
        key.CreateDate = ExpirationCutoff.Subtract(TimeSpan.FromMinutes(10));
        key.Status = StatusType.Active;

        _mocker.GetMock<IAccessKeyRepository>().Setup(x => x.GetByIdAsync(It.IsAny<string>()))
            .ReturnsAsync(new AccessKeyEntity { Id = _fixture.Create<string>() });
        
        // act
        var result = await _classUnderTest.FunctionHandler(
            new List<AccessKey> { key }, new TestLambdaContext());

        // assert & verify
        Assert.Equal(1, result.Count);
        Assert.Contains(result, a =>
            (a.Action == ActionType.Rotate && a.AccessKeyId == key.AccessKeyId));
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

    public TimeSpan AccessKeyRotationWindow() =>
        DateTime.UtcNow - _expiationCutoff;

    public TimeSpan AccessKeyInstallationWindow() =>
        _installationCutoff - _expiationCutoff;

    public TimeSpan AccessKeyRecoveryWindow() =>
        _recoveryCutoff - _installationCutoff;
} 