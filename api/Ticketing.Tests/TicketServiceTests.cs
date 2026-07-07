using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Ticketing.Data;
using Ticketing.Services.Comments;
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
            new UpdateTicketRequest(teamId, "bug", null, "T", "B", "new"),
            user);

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
            new UpdateTicketRequest(teamId, "feature", null, "T2", "B", "in_progress"),
            user);

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
            new UpdateTicketRequest(teamB, "bug", epicA, "T", "B", "new"),
            user);

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
        var result = await tickets.UpdateStateAsync(created.Id, new UpdateTicketStateRequest("done"), user);

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

        var result = await tickets.UpdateStateAsync(created.Id, new UpdateTicketStateRequest("archived"), user);

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

        var all = (await tickets.ListAsync(new TicketQuery(teamId, null, null, null))).Value;
        Assert.Equal(second.Id, all[0].Id); // most recently modified first

        var bugs = (await tickets.ListAsync(new TicketQuery(teamId, "bug", null, null))).Value;
        Assert.Single(bugs);
        Assert.Equal(first.Id, bugs[0].Id);

        var search = (await tickets.ListAsync(new TicketQuery(teamId, null, null, "beta"))).Value;
        Assert.Single(search);
        Assert.Equal(second.Id, search[0].Id);
    }

    [Fact]
    public async Task List_Invalid_Type_Returns_Validation()
    {
        using var db = TestSupport.NewDb();
        var user = TestSupport.AddUser(db);
        var teamId = await NewTeam(db);
        var tickets = TestSupport.NewTicketService(db);
        await tickets.CreateAsync(new CreateTicketRequest(teamId, "bug", null, "T", "B"), user);

        // A non-empty, unparseable type must be rejected rather than silently ignored.
        var result = await tickets.ListAsync(new TicketQuery(teamId, "task", null, null));

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Validation, result.FirstError.Type);
    }

    [Fact]
    public async Task List_Valid_Type_Filters_Correctly()
    {
        using var db = TestSupport.NewDb();
        var user = TestSupport.AddUser(db);
        var teamId = await NewTeam(db);
        var tickets = TestSupport.NewTicketService(db);
        var bug = (await tickets.CreateAsync(new CreateTicketRequest(teamId, "bug", null, "Bug", "B"), user)).Value;
        await tickets.CreateAsync(new CreateTicketRequest(teamId, "feature", null, "Feature", "B"), user);

        var result = await tickets.ListAsync(new TicketQuery(teamId, "bug", null, null));

        Assert.False(result.IsError);
        Assert.Single(result.Value);
        Assert.Equal(bug.Id, result.Value[0].Id);
    }

    [Fact]
    public async Task List_Empty_Type_Returns_All()
    {
        using var db = TestSupport.NewDb();
        var user = TestSupport.AddUser(db);
        var teamId = await NewTeam(db);
        var tickets = TestSupport.NewTicketService(db);
        await tickets.CreateAsync(new CreateTicketRequest(teamId, "bug", null, "A", "B"), user);
        await tickets.CreateAsync(new CreateTicketRequest(teamId, "feature", null, "C", "D"), user);

        // Whitespace/empty type means "no filter".
        var result = await tickets.ListAsync(new TicketQuery(teamId, "   ", null, null));

        Assert.False(result.IsError);
        Assert.Equal(2, result.Value.Count);
    }

    [Fact]
    public async Task Update_Records_History_For_Changed_Fields()
    {
        using var db = TestSupport.NewDb();
        var user = TestSupport.AddUser(db);
        var teamId = await NewTeam(db);
        var tickets = TestSupport.NewTicketService(db);
        var created = (await tickets.CreateAsync(new CreateTicketRequest(teamId, "bug", null, "Old title", "B"), user)).Value;

        var result = await tickets.UpdateAsync(
            created.Id,
            new UpdateTicketRequest(teamId, "feature", null, "New title", "B", "in_progress"),
            user);

        Assert.False(result.IsError);

        var history = await db.TicketHistory.Where(h => h.TicketId == created.Id).ToListAsync();

        // Only the three fields that actually changed (title, type, state) are logged.
        Assert.Equal(3, history.Count);
        Assert.All(history, h => Assert.Equal(user, h.ChangedBy));

        var title = Assert.Single(history, h => h.Field == "title");
        Assert.Equal("Old title", title.OldValue);
        Assert.Equal("New title", title.NewValue);

        var type = Assert.Single(history, h => h.Field == "type");
        Assert.Equal("bug", type.OldValue);
        Assert.Equal("feature", type.NewValue);

        var state = Assert.Single(history, h => h.Field == "state");
        Assert.Equal("new", state.OldValue);
        Assert.Equal("in_progress", state.NewValue);
    }

    [Fact]
    public async Task Update_NoOp_Records_No_History()
    {
        using var db = TestSupport.NewDb();
        var user = TestSupport.AddUser(db);
        var teamId = await NewTeam(db);
        var tickets = TestSupport.NewTicketService(db);
        var created = (await tickets.CreateAsync(new CreateTicketRequest(teamId, "bug", null, "T", "B"), user)).Value;

        // Same values as the created ticket -> nothing changes.
        await tickets.UpdateAsync(
            created.Id,
            new UpdateTicketRequest(teamId, "bug", null, "T", "B", "new"),
            user);

        Assert.Equal(0, await db.TicketHistory.CountAsync());
    }

    [Fact]
    public async Task Update_Records_Readable_Epic_And_Team_Values()
    {
        using var db = TestSupport.NewDb();
        var user = TestSupport.AddUser(db);
        var teamA = await NewTeam(db, "Alpha");
        var teamB = await NewTeam(db, "Beta");
        var epicB = await NewEpic(db, teamB, "Checkout revamp");
        var tickets = TestSupport.NewTicketService(db);
        var created = (await tickets.CreateAsync(new CreateTicketRequest(teamA, "bug", null, "T", "B"), user)).Value;

        // Move to team B and attach an epic that belongs to team B.
        var result = await tickets.UpdateAsync(
            created.Id,
            new UpdateTicketRequest(teamB, "bug", epicB, "T", "B", "new"),
            user);

        Assert.False(result.IsError);

        var history = await db.TicketHistory.Where(h => h.TicketId == created.Id).ToListAsync();

        var team = Assert.Single(history, h => h.Field == "team");
        Assert.Equal("Alpha", team.OldValue);
        Assert.Equal("Beta", team.NewValue);

        var epic = Assert.Single(history, h => h.Field == "epic");
        Assert.Equal("None", epic.OldValue);
        Assert.Equal("Checkout revamp", epic.NewValue);
    }

    [Fact]
    public async Task UpdateState_Records_State_History_Entry()
    {
        using var db = TestSupport.NewDb();
        var user = TestSupport.AddUser(db);
        var teamId = await NewTeam(db);
        var tickets = TestSupport.NewTicketService(db);
        var created = (await tickets.CreateAsync(new CreateTicketRequest(teamId, "bug", null, "T", "B"), user)).Value;

        await tickets.UpdateStateAsync(created.Id, new UpdateTicketStateRequest("done"), user);

        var entry = Assert.Single(await db.TicketHistory.Where(h => h.TicketId == created.Id).ToListAsync());
        Assert.Equal("state", entry.Field);
        Assert.Equal("new", entry.OldValue);
        Assert.Equal("done", entry.NewValue);
        Assert.Equal(user, entry.ChangedBy);
    }

    [Fact]
    public async Task Adding_Comment_Records_No_History()
    {
        using var db = TestSupport.NewDb();
        var user = TestSupport.AddUser(db);
        var teamId = await NewTeam(db);
        var tickets = TestSupport.NewTicketService(db);
        var created = (await tickets.CreateAsync(new CreateTicketRequest(teamId, "bug", null, "T", "B"), user)).Value;

        await TestSupport.NewCommentService(db).AddAsync(created.Id, new CreateCommentRequest("A comment"), user);

        Assert.Equal(0, await db.TicketHistory.CountAsync());
    }

    [Fact]
    public async Task GetHistory_Returns_Entries_Newest_First()
    {
        using var db = TestSupport.NewDb();
        var user = TestSupport.AddUser(db);
        var teamId = await NewTeam(db);
        var tickets = TestSupport.NewTicketService(db);
        var created = (await tickets.CreateAsync(new CreateTicketRequest(teamId, "bug", null, "T", "B"), user)).Value;

        await tickets.UpdateStateAsync(created.Id, new UpdateTicketStateRequest("in_progress"), user);
        await Task.Delay(5);
        await tickets.UpdateStateAsync(created.Id, new UpdateTicketStateRequest("done"), user);

        var result = await tickets.GetHistoryAsync(created.Id);

        Assert.False(result.IsError);
        Assert.Equal(2, result.Value.Count);
        // Newest change first.
        Assert.Equal("done", result.Value[0].NewValue);
        Assert.Equal("in_progress", result.Value[1].NewValue);
        Assert.Equal("Test User", result.Value[0].ChangedByName);
    }

    [Fact]
    public async Task GetHistory_Missing_Ticket_Returns_NotFound()
    {
        using var db = TestSupport.NewDb();
        var tickets = TestSupport.NewTicketService(db);

        var result = await tickets.GetHistoryAsync(Guid.NewGuid());

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
    }
}
