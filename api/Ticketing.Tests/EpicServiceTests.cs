using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Ticketing.Services.Epics;
using Ticketing.Services.Teams;
using Xunit;

namespace Ticketing.Tests;

public class EpicServiceTests
{
    private static async Task<Guid> NewTeamId(Ticketing.Data.TicketingDbContext db, string name = "Alpha")
    {
        var team = (await TestSupport.NewTeamService(db).CreateAsync(new CreateTeamRequest(name))).Value;
        return team.Id;
    }

    [Fact]
    public async Task Create_Epic_Under_Existing_Team_Succeeds()
    {
        using var db = TestSupport.NewDb();
        var teamId = await NewTeamId(db);
        var epics = TestSupport.NewEpicService(db);

        var result = await epics.CreateAsync(new CreateEpicRequest(teamId, "  My Epic  ", "desc"));

        Assert.False(result.IsError);
        Assert.Equal("My Epic", result.Value.Title);
        Assert.Equal(teamId, result.Value.TeamId);
    }

    [Fact]
    public async Task Create_Epic_Under_Missing_Team_Returns_NotFound()
    {
        using var db = TestSupport.NewDb();
        var epics = TestSupport.NewEpicService(db);

        var result = await epics.CreateAsync(new CreateEpicRequest(Guid.NewGuid(), "Epic", null));

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
    }

    [Fact]
    public async Task Update_Epic_Keeps_Team()
    {
        using var db = TestSupport.NewDb();
        var teamId = await NewTeamId(db);
        var epics = TestSupport.NewEpicService(db);
        var epic = (await epics.CreateAsync(new CreateEpicRequest(teamId, "Epic", null))).Value;

        var result = await epics.UpdateAsync(epic.Id, new UpdateEpicRequest("Renamed", "new desc"));

        Assert.False(result.IsError);
        Assert.Equal("Renamed", result.Value.Title);
        Assert.Equal(teamId, result.Value.TeamId);
    }

    [Fact]
    public async Task Delete_Unreferenced_Epic_Succeeds()
    {
        using var db = TestSupport.NewDb();
        var teamId = await NewTeamId(db);
        var epics = TestSupport.NewEpicService(db);
        var epic = (await epics.CreateAsync(new CreateEpicRequest(teamId, "Epic", null))).Value;

        var result = await epics.DeleteAsync(epic.Id);

        Assert.False(result.IsError);
        Assert.Equal(0, await db.Epics.CountAsync());
    }

    [Fact]
    public async Task Delete_Referenced_Epic_Returns_Conflict()
    {
        using var db = TestSupport.NewDb();
        var teamId = await NewTeamId(db);
        var epics = TestSupport.NewEpicService(db);
        var epic = (await epics.CreateAsync(new CreateEpicRequest(teamId, "Epic", null))).Value;
        TestSupport.AddTicket(db, teamId, epic.Id);

        var result = await epics.DeleteAsync(epic.Id);

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Conflict, result.FirstError.Type);
    }

    [Fact]
    public async Task List_Returns_Only_Selected_Teams_Epics()
    {
        using var db = TestSupport.NewDb();
        var teamA = await NewTeamId(db, "Alpha");
        var teamB = await NewTeamId(db, "Beta");
        var epics = TestSupport.NewEpicService(db);
        await epics.CreateAsync(new CreateEpicRequest(teamA, "A1", null));
        await epics.CreateAsync(new CreateEpicRequest(teamA, "A2", null));
        await epics.CreateAsync(new CreateEpicRequest(teamB, "B1", null));

        var list = await epics.ListAsync(teamA);

        Assert.Equal(2, list.Count);
        Assert.All(list, e => Assert.Equal(teamA, e.TeamId));
    }
}
