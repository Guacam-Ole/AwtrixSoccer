namespace SoccerUlanzi;

public class Fake
{
    private readonly AwTrix _awTrix;

    public Fake(AwTrix awTrix)
    {
        _awTrix = awTrix;
    }
    public async Task Demo()
    {
        await SendFakeBefore();
        Thread.Sleep(TimeSpan.FromSeconds(10));
        await SendFakeEvent(4, AwTrix.GamesStates.Playing, 0,0);
        Thread.Sleep(TimeSpan.FromSeconds(10));
        await SendFakeEvent(45, AwTrix.GamesStates.Pause, 2,0);
        Thread.Sleep(TimeSpan.FromSeconds(10));
        await SendFakeEvent(102, AwTrix.GamesStates.OverTime, 2,2);
        Thread.Sleep(TimeSpan.FromSeconds(10));
        await SendFakeEvent(102, AwTrix.GamesStates.Finished, 12,2);
        Thread.Sleep(TimeSpan.FromSeconds(10));
    }

    private async Task SendFakeBefore()
    {
        var homeTeam = FakeTeam("270", 0);
        var guestTeam = FakeTeam("127", 0);

        var date = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 20, 30,0);
        if (date < DateTime.Now) date = date.AddDays(1);
        await _awTrix.ShowPreview(homeTeam, guestTeam, date, "270");
    }

    private static Team FakeTeam(string teamId, int goals)
    {
        return new Team
        {
            Goals = goals,
            IconPath = $"./cache/{teamId}.png",
            IconUrl = ""
        };
    }
    
    private async Task SendFakeEvent(int minutesIntoGame, AwTrix.GamesStates state, int homeGoals, int guestGoals)
    {
        var homeTeam = FakeTeam("270", homeGoals);
        var guestTeam = FakeTeam("127", guestGoals);

        await _awTrix.SendNewStandings(homeTeam, guestTeam, minutesIntoGame, state, "270");
    }
}