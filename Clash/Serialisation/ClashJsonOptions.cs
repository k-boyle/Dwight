using System.Text.Json;
using System.Text.Json.Serialization;

namespace Dwight;

internal static class ClashJsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };
}
