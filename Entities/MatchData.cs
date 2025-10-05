namespace SoccerUlanzi.Entities;

public class MatchData
{
    public int Minute { get; set; }
    private FixtureTeam Home { get; set; }
    private FixtureTeam Guest { get; set; }
}