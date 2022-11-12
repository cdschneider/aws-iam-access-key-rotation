using System.Text;
using States.DateTimeAdd.Serialization;
using Xunit;

namespace States.DateTimeAdd.Tests;

public class InputDeserializationTest
{
    [Fact]
    public void TestDeserialization_WhenUnitSpecifiedAsName_ThenDeserializationSucceeds()
    {
        var input = "{\"Step\":30,\"Unit\":\"Days\",\"Value\":\"1970-01-01T00:00:00Z\"}";
        var serializer = new CustomLambdaSerializer();

        using var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(input));
        var result = serializer.Deserialize<DateTimeAddRequest>(memoryStream);
        
        Assert.Equal(30, result.Step);
        Assert.Equal(TimeSpanUnit.Days, result.Unit);
        Assert.Equal(new DateTime(1970, 1, 1), result.Value);
    }
    
    [Fact]
    public void TestDeserialization_WhenUnitOrdinalUsed_ThenExceptionIsThrown()
    {
        var input = "{\"Step\":100,\"Unit\":1,\"Value\":\"1970-01-01T00:00:00Z\"}";
        var serializer = new CustomLambdaSerializer();

        using var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(input));
        Assert.ThrowsAny<Exception>(() => serializer.Deserialize<DateTimeAddRequest>(memoryStream));
    }
    
    [Fact]
    public void TestDeserialization_WhenNoUnitSpecified_ThenTicksIsUsed()
    {
        var input = "{\"Step\":100,\"Value\":\"1970-01-01T00:00:00Z\"}";
        var serializer = new CustomLambdaSerializer();

        using var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(input));
        var result = serializer.Deserialize<DateTimeAddRequest>(memoryStream);
        
        Assert.Equal(100, result.Step);
        Assert.Equal(new DateTime(1970, 1, 1), result.Value);
        Assert.Equal(TimeSpanUnit.Ticks, result.Unit);
    }
}