using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda.Serialization.SystemTextJson;

namespace AccessKeyActions.Serialization;

public class CustomLambdaSerializer : DefaultLambdaJsonSerializer
{
    public CustomLambdaSerializer() : base(CreateCustomizer()) { }

    private static Action<JsonSerializerOptions> CreateCustomizer()
    {
        return (JsonSerializerOptions options) =>
        {
            options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, false));
            options.Converters.Add(new StatusTypeConverter());
        };
    }
}