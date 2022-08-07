using System.Collections.Generic;

namespace ClashWrapper.Models.League;

public class LeagueGroup
{
    public State State { get; set; }
    public List<Round> Rounds { get; set; }
}
