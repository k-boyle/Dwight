using System;
using System.Text.Json.Serialization;

namespace Dwight;

public record CurrentWar(
    WarState State,
    [property: JsonConverter(typeof(DateTimeOffsetDeserializer))]
    DateTimeOffset PreparationStartTime,
    [property: JsonConverter(typeof(DateTimeOffsetDeserializer))]
    DateTimeOffset EndTime,
    WarClan Clan,
    WarClan Opponent);
