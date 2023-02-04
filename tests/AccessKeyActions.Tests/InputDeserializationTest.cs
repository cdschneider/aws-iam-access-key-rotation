using System.Text;
using AccessKeyActions.Serialization;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Xunit;

namespace AccessKeyActions.Tests;

public class InputDeserializationTest
{
    [Fact]
    public void TestDeserialization_WhenStatusSpecifiedAsName_ThenDeserializationSucceeds()
    {
        var input = "[{\"AccessKeyId\": \"a-fake-access-key123\",\"CreateDate\":\"2023-01-15T18:18:05Z\",\"Status\":\"Active\",\"UserName\":\"a-username\"}]";
        var serializer = new CustomLambdaSerializer();

        using var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(input));
        var result = serializer.Deserialize<List<AccessKey>>(memoryStream);
        
        Assert.Single(result);
        Assert.Contains(result, key => key.Status == StatusType.Active);
    }
}