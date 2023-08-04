using Amazon.CDK;
using Constructs;

namespace IamAccessKeyRotation.Cdk;

public class IamAccessKeyRotationStack : Stack
{
    public IamAccessKeyRotationStack(
        Construct scope,
        string id,
        IStackProps props = null
    ) : base(scope, id, props)
    {
        
    }
}