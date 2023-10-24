namespace AccessKeyActions.Options;

public class AccessKeyActionsOptions
{
    public const string AccessKeyActionsOptionsSection = "AccessKeyActions";
    
    public TimeSpan KeyRotation { get; set; } = TimeSpan.FromDays(30);
    public TimeSpan KeyInstallation { get; set; } = TimeSpan.FromDays(7);
    public TimeSpan KeyRecovery { get; set; } = TimeSpan.FromDays(7);
}