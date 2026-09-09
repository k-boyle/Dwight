using System;
using System.Text.Json.Serialization;

namespace Dwight;

public class ApiFailure : Exception
{
    [JsonPropertyName("reason")]
    public string Reason { get; }

    [JsonPropertyName("message")]
    public new string Message { get; }

    [JsonPropertyName("type")]
    public string Type { get; }

    public ApiFailure(string reason, string message, string type)
    {
        Reason = reason;
        Message = message;
        Type = type;
    }
}
