namespace AccessKeyActions.Options;

public class DynamoDBOptions
{
    public const string DynamoDBOptionsSection = "DynamoDB";
    
    public string TableName { get; set; }
    
    public string IndexName { get; set; }
}