using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Ticketing.Data;
using Ticketing.Services.Epics;
using Ticketing.Services.Teams;
using Ticketing.Services.Tickets;
using Xunit;

namespace Ticketing.Tests;

public class TicketServiceTests
{
    private static async Task<Guid> NewTeam(TicketingDbContext db, string name = "Alpha")
        => (await TestSupport.NewTeamService(db).CreateAsync(new CreateTeamRequest(name))).Value.Id;

    private static async Task<Guid> NewEpic(TicketingDbContext db, Guid teamId, string title = "Epic")
        => (await TestSupport.NewEpicService(db).CreateAsync(new CreateEpicRequest(teamId, title, null))).Value.Id;

    [Fact]
    public async Task Create_Sets_New_State_And_Creator()
    {
        using var db = TestSupport.NewDb();
        var user = TestSupport.AddUser(db);
        var teamId = await NewTeam(db);
        var tickets = TestSupport.NewTicketService(db);

        var result = await tickets.CreateAsync(new CreateTicketRequest(teamId, "bug", null, "  Title  ", "Body"), user);

        Assert.False(result.IsError);
        Assert.Equal("new", result.Value.State);
        Assert.Equal("Title", result.Value.Title);
        Assert.Equal(user, result.Value.CreatedBy);
    }

    [Fact]
    public async Task Create_Invalid_Type_Returns_Validation()
    {
        using var db = TestSupport.NewDb();
        var user = TestSupport.AddUser(db);
        var teamId = await NewTeam(db);
        var tickets = TestSupport.NewTicketService(db);

        var result = await tickets.CreateAsync(new CreateTicketRequest(teamId, "task", null, "T", "B"), user);

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Validation, result.FirstError.Type);
    }

    [Fact]
    public async Task Create_With_Epic_From_Other_Team_Is_Rejected()
    {
        using var db = TestSupport.NewDb();
        var user = TestSupport.AddUser(db);
        var teamA = await NewTeam(db, "Alpha");
        var teamB = await NewTeam(db, "Beta");
        var epicB = await NewEpic(db, teamB);
        var tickets = TestSupport.NewTicketService(db);

        var result = await tickets.CreateAsync(new CreateTicketRequest(teamA, "bug", epicB, "T", "B"), user);

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Validation, result.FirstError.Type);
    }

    [Fact]
    public async Task Update_NoOp_Does_Not_Advance_ModifiedAt()
    {
        using var db = TestSupport.NewDb();
        var user = TestSupport.AddUser(db);
        var teamId = await NewTeam(db);
        var tickets = TestSupport.NewTicketService(db);
        var created = (await tickets.CreateAsync(new CreateTicketRequest(teamId, "bug", null, "T", "B"), user)).Value;

        var result = await tickets.UpdateAsync(
            created.Id,
            new UpdateTicketRequest(teamId, "bug", null, "T", "B", "new"));

        Assert.False(result.IsError);
        Assert.Equal(created.ModifiedAt, result.Value.ModifiedAt);
    }

    [Fact]
    public async Task Update_With_Change_Advances_ModifiedAt()
    {
        using var db = TestSupport.NewDb();
        var user = TestSupport.AddUser(db);
        var teamId = await NewTeam(db);
        var tickets = TestSupport.NewTicketService(db);
        var created = (await tickets.CreateAsync(new CreateTicketRequest(teamId, "bug", null, "T", "B"), user)).Value;

        await Task.Delay(5);
        var result = await tickets.UpdateAsync(
            created.Id,
            new UpdateTicketRequest(teamId, "feature", null, "T2", "B", "in_progress"));

        Assert.False(result.IsError);
        Assert.True(result.Value.ModifiedAt > created.ModifiedAt);
        Assert.Equal("feature", result.Value.Type);
        Assert.Equal("in_progress", result.Value.State);
    }

    [Fact]
    public async Task Update_Rejects_Epic_From_Different_Team_When_Team_Changes()
    {
        using var db = TestSupport.NewDb();
        var user = TestSupport.AddUser(db);
        var teamA = await NewTeam(db, "Alpha");
        var teamB = await NewTeam(db, "Beta");
        var epicA = await NewEpic(db, teamA);
        var tickets = TestSupport.NewTicketService(db);
        var created = (await tickets.CreateAsync(new CreateTicketRequest(teamA, "bug", epicA, "T", "B"), user)).Value;

        // Move to team B but keep epic from team A -> must be rejected.
        var result = await tickets.UpdateAsync(
            created.Id,
            new UpdateTicketRequest(teamB, "bug", epicA, "T", "B", "new"));

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Validation, result.FirstError.Type);
    }

    [Fact]
    public async Task UpdateState_Changes_State_And_Advances_ModifiedAt()
    {
        using var db = TestSupport.NewDb();
        var user = TestSupport.AddUser(db);
        var teamId = await NewTeam(db);
        var tickets = TestSupport.NewTicketService(db);
        var created = (await tickets.CreateAsync(new CreateTicketRequest(teamId, "bug", null, "T", "B"), user)).Value;

        await Task.Delay(5);
        var result = await tickets.UpdateStateAsync(created.Id, new UpdateTicketStateRequest("done"));

        Assert.False(result.IsError);
        Assert.Equal("done", result.Value.State);
        Assert.True(result.Value.ModifiedAt > created.ModifiedAt);
    }

    [Fact]
    public async Task UpdateState_Invalid_Returns_Validation()
    {
        using var db = TestSupport.NewDb();
        var user = TestSupport.AddUser(db);
        var teamId = await NewTeam(db);
        var tickets = TestSupport.NewTicketService(db);
        var created = (await tickets.CreateAsync(new CreateTicketRequest(teamId, "bug", null, "T", "B"), user)).Value;

        var result = await tickets.UpdateStateAsync(created.Id, new UpdateTicketStateRequest("archived"));

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Validation, result.FirstError.Type);
    }

    [Fact]
    public async Task Delete_Removes_Ticket()
    {
        using var db = TestSupport.NewDb();
        var user = TestSupport.AddUser(db);
        var teamId = await NewTeam(db);
        var tickets = TestSupport.NewTicketService(db);
        var created = (await tickets.CreateAsync(new CreateTicketRequest(teamId, "bug", null, "T", "B"), user)).Value;

        var result = await tickets.DeleteAsync(created.Id);

        Assert.False(result.IsError);
        Assert.Equal(0, await db.Tickets.CountAsync());
    }

    [Fact]
    public async Task List_Filters_And_Orders_By_ModifiedAt_Desc()
    {
        using var db = TestSupport.NewDb();
        var user = TestSupport.AddUser(db);
        var teamId = await NewTeam(db);
        var tickets = TestSupport.NewTicketService(db);
        var first = (await tickets.CreateAsync(new CreateTicketRequest(teamId, "bug", null, "Alpha bug", "B"), user)).Value;
        await Task.Delay(5);
        var second = (await tickets.CreateAsync(new CreateTicketRequest(teamId, "feature", null, "Beta feature", "B"), user)).Value;

        var all = await tickets.ListAsync(new TicketQuery(teamId, null, null, null));
        Assert.Equal(second.Id, all[0].Id); // most recently modified first

        var bugs = await tickets.ListAsync(new TicketQuery(teamId, "bug", null, null));
        Assert.Single(bugs);
        Assert.Equal(first.Id, bugs[0].Id);

        var search = await tickets.ListAsync(new TicketQuery(teamId, null, null, "beta"));
        Assert.Single(search);
        Assert.Equal(second.Id, search[0].Id);
    }
}
