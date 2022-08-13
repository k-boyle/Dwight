using Newtonsoft.Json;

namespace Dwight;

public enum WarState
{
    [JsonProperty("clanNotFound")]
    ClanNotFound,
    
    [JsonProperty("accessDenied")]
    AccessDenied,
    
    [JsonProperty("notInWar")]
    NotInWar,
    
    [JsonProperty("inMatchmaking")]
    InMatchmaking,
    
    [JsonProperty("enterWar")]
    EnterWar,
    
    [JsonProperty("matched")]
    Matched,
    
    [JsonProperty("preparation")]
    Preparation,
    
    [JsonProperty("war")]
    War,
    
    [JsonProperty("inWar")]
    InWar,
    
    [JsonProperty("ended")]
    Ended
}
