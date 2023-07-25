namespace AccessKeyRotation.Models;

public class LambdaFunctionArn
{
    public string AccountId { get; set; }
    
    public string FunctionName { get; set; }

    public string Alias { get; set; } = "$LATEST";
    
    public int? Version { get; set; }

    public bool Qualified => Version.HasValue;
}