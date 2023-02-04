using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.IdentityManagement;

namespace AccessKeyActions.Serialization;

public class StatusTypeConverter : JsonConverter<StatusType>
{
    public override StatusType Read(
        ref Utf8JsonReader reader, 
        Type typeToConvert, 
        JsonSerializerOptions options) => 
            StatusType.FindValue(reader.GetString());

    public override void Write(Utf8JsonWriter writer, StatusType value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}