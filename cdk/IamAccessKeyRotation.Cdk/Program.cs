using Amazon.CDK;

namespace IamAccessKeyRotation.Cdk;

class Program
{
    static void Main(string[] args)
    {
        var app = new App();

        new IamAccessKeyRotationStack(app, "IamAccessKeyRotationStack");

        app.Synth();
    }
}
