namespace AccessKeyRotation;

public static class Constants
{
    public static readonly string SecretNameFormat = "iam/{0}/accesskey";
    public static readonly string PolicyDocumentFormat = @"
{
    ""Version"": ""2012-10-17"",
    ""Statement"": [
        {
            ""Effect"":""Allow"",
            ""Principal"": {
                ""AWS"":""arn:aws:iam::{0}:user/{1}""
            },
            ""Action"": [
                ""secretsmanager:GetSecretValue"",
                ""secretsmanager:DescribeSecret"",
                ""secretsmanager:ListSecretVersionIds"",
                ""secretsmanager:ListSecrets""
            ],
            ""Resource"": ""*""
        }
    ]
}
";
}