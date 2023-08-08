using System.Collections.Generic;
using Amazon.CDK;
using Amazon.CDK.AWS.StepFunctions;
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
        _ = new StateMachine(scope, "StateMachine", new StateMachineProps
        {
            DefinitionBody = DefinitionBody.FromFile(""), //TODO path to template
            DefinitionSubstitutions = new Dictionary<string, string>
            {
                ["AccessKeyActionsFunctionArn"] = "",
                ["AccessKeyRotationFunctionArn"] = "",
                ["AccessKeyDynamoDbTableName"] = ""
            },
            Logs = new LogOptions
            {
                //TODO add details
            },
            StateMachineType = StateMachineType.EXPRESS,
            Timeout = Duration.Seconds(120),
            TracingEnabled = true
        });
    }
}