using System.Text.RegularExpressions;
using AccessKeyRotation.Models;

namespace AccessKeyRotation.Services;

public class LambdaFunctionArnParser : ILambdaFunctionArnParser
{
    private static readonly string LambdaArnPattern =
        @"arn:(aws[a-zA-Z-]*):lambda:([a-z]{2}(-gov)?-[a-z]+-\d{1}):(\d{12}):function:([a-zA-Z0-9-_]+)(:(\$LATEST|[a-zA-Z0-9-_]+))?";
    
    public LambdaFunctionArn Parse(string input)
    {
        if (input == null) throw new ArgumentNullException(nameof(input));
        if (string.IsNullOrEmpty(input)) throw new ArgumentException(nameof(input));
        
        Regex regex = new Regex(LambdaArnPattern);
        Match m = regex.Match(input);

        if (m.Success)
        {
            return new LambdaFunctionArn { AccountId = m.Groups[3].Value };
        }

        throw new ArgumentException("Input did not meet requirements such that it could be properly parsed. " +
                                    "Expected valid a valid Lambda Function ARN input", nameof(input));
    }
}