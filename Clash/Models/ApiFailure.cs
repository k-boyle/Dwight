using System;
using Newtonsoft.Json;

namespace Dwight;

public class ApiFailure : Exception
{
    [JsonProperty("reason")]
    public string Reason { get; }

    [JsonProperty("message")]
    public new string Message { get; }

    [JsonProperty("type")]
    public string Type { get; }

    public ApiFailure(string reason, string message, string type)
    {
        Reason = reason;
        Message = message;
        Type = type;
    }
}