using System;
using System.Globalization;
using Newtonsoft.Json;

namespace Dwight;

public class DateTimeOffsetDeserializer : JsonConverter<DateTimeOffset>
{
    public override void WriteJson(JsonWriter writer, DateTimeOffset value, JsonSerializer serializer)
        => throw new NotImplementedException();

    public override DateTimeOffset ReadJson(JsonReader reader, Type objectType, DateTimeOffset existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        return DateTimeOffset.ParseExact((string)reader.Value!, "yyyyMMdd'T'HHmmss.fff'Z'", CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
    }
}