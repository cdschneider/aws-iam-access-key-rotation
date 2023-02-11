namespace AccessKeyActions.Configuration;

public interface IFunctionConfiguration
{
    /// <summary>
    /// Period for which an AccessKey should exist before it is rotated
    /// </summary>
    /// <returns></returns>
    TimeSpan AccessKeyRotationWindow();

    /// <summary>
    /// Period before a rotated AccessKey should be deactivated 
    /// </summary>
    /// <returns></returns>
    TimeSpan AccessKeyInstallationWindow();

    /// <summary>
    /// Period before a deactivated AccessKey should be permanently deleted
    /// </summary>
    /// <returns></returns>
    TimeSpan AccessKeyRecoveryWindow();
}