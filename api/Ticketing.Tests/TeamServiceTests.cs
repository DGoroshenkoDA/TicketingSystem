using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Ticketing.Services.Teams;
using Xunit;

namespace Ticketing.Tests;

public class TeamServiceTests
{
    [Fact]
    public async Task Create_Team_Persists_And_Normalizes_Name()
    {
        using var db = TestSupport.NewDb();
        var service = TestSupport.NewTeamService(db);

        var result = await service.CreateAsync(new CreateTeamRequest("  Alpha  "));

        Assert.False(result.IsError);
        Assert.Equal("Alpha", result.Value.Name);
        var team = await db.Teams.SingleAsync();
        Assert.Equal("alpha", team.NameNormalized);
    }

    [Fact]
    public async Task Create_Duplicate_Name_Returns_Conflict()
    {
        using var db = TestSupport.NewDb();
        var service = TestSupport.NewTeamService(db);
        await service.CreateAsync(new CreateTeamRequest("Alpha"));

        var second = await service.CreateAsync(new CreateTeamRequest("  alpha "));

        Assert.True(second.IsError);
        Assert.Equal(ErrorType.Conflict, second.FirstError.Type);
    }

    [Fact]
    public async Task Rename_Team_Updates_Name()
    {
        using var db = TestSupport.NewDb();
        var service = TestSupport.NewTeamService(db);
        var team = (await service.CreateAsync(new CreateTeamRequest("Alpha"))).Value;

        var result = await service.UpdateAsync(team.Id, new UpdateTeamRequest("Beta"));

        Assert.False(result.IsError);
        Assert.Equal("Beta", result.Value.Name);
    }

    [Fact]
    public async Task Delete_Empty_Team_Succeeds()
    {
        using var db = TestSupport.NewDb();
        var service = TestSupport.NewTeamService(db);
        var team = (await service.CreateAsync(new CreateTeamRequest("Alpha"))).Value;

        var result = await service.DeleteAsync(team.Id);

        Assert.False(result.IsError);
        Assert.Equal(0, await db.Teams.CountAsync());
    }

    [Fact]
    public async Task Delete_Team_With_Epic_Returns_Conflict()
    {
        using var db = TestSupport.NewDb();
        var teams = TestSupport.NewTeamService(db);
        var epics = TestSupport.NewEpicService(db);
        var team = (await teams.CreateAsync(new CreateTeamRequest("Alpha"))).Value;
        await epics.CreateAsync(new Ticketing.Services.Epics.CreateEpicRequest(team.Id, "Epic", null));

        var result = await teams.DeleteAsync(team.Id);

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Conflict, result.FirstError.Type);
    }

    [Fact]
    public async Task Delete_Team_With_Ticket_Returns_Conflict()
    {
        using var db = TestSupport.NewDb();
        var teams = TestSupport.NewTeamService(db);
        var team = (await teams.CreateAsync(new CreateTeamRequest("Alpha"))).Value;
        TestSupport.AddTicket(db, team.Id);

        var result = await teams.DeleteAsync(team.Id);

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Conflict, result.FirstError.Type);
    }

    [Fact]
    public async Task Get_Unknown_Team_Returns_NotFound()
    {
        using var db = TestSupport.NewDb();
        var service = TestSupport.NewTeamService(db);

        var result = await service.GetAsync(Guid.NewGuid());

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
    }
}
