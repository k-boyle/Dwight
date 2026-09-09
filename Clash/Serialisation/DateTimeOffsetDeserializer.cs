using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dwight;

public class DateTimeOffsetDeserializer : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return DateTimeOffset.ParseExact(reader.GetString()!, "yyyyMMdd'T'HHmmss.fff'Z'", CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
        => throw new NotImplementedException();
}
