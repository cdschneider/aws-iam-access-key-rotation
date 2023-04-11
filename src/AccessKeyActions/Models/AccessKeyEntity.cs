using Amazon.DynamoDBv2.DataModel;

namespace AccessKeyActions.Models;

[DynamoDBTable("")]
public class AccessKeyEntity
{
    [DynamoDBHashKey]
    public string Id { get; set; }
    
    public DateTime? RotationDate { get; set; }
    
    public DateTime? DeactivationDate { get; set; }
    
    public DateTime? DeletionDate { get; set; }
}