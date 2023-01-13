using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda.Serialization.SystemTextJson;

namespace States.DateTimeAdd.Serialization;

public class CustomLambdaSerializer : DefaultLambdaJsonSerializer
{
    public CustomLambdaSerializer() : base(CreateCustomizer()) { }

    public static Action<JsonSerializerOptions> CreateCustomizer()
    {
        return (JsonSerializerOptions options) =>
        {
            options.Converters.Add(
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, false));
        };
    }
}