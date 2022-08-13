using System;
using Newtonsoft.Json;

namespace Dwight;

public record CurrentWar(
    WarState State,
    [JsonConverter(typeof(DateTimeOffsetDeserializer))]
    DateTimeOffset EndTime,
    WarClan Clan,
    WarClan Opponent);