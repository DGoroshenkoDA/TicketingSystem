using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Ticketing.Data;
using Ticketing.Services.Comments;
using Ticketing.Services.Teams;
using Ticketing.Services.Tickets;
using Xunit;

namespace Ticketing.Tests;

public class CommentServiceTests
{
    private static async Task<(Guid ticketId, Guid userId, DateTime modifiedAt)> Seed(TicketingDbContext db)
    {
        var user = TestSupport.AddUser(db);
        var teamId = (await TestSupport.NewTeamService(db).CreateAsync(new CreateTeamRequest("Alpha"))).Value.Id;
        var ticket = (await TestSupport.NewTicketService(db)
            .CreateAsync(new CreateTicketRequest(teamId, "bug", null, "T", "B"), user)).Value;
        return (ticket.Id, user, ticket.ModifiedAt);
    }

    [Fact]
    public async Task Add_Comment_Returns_Dto_With_Author()
    {
        using var db = TestSupport.NewDb();
        var (ticketId, userId, _) = await Seed(db);
        var comments = TestSupport.NewCommentService(db);

        var result = await comments.AddAsync(ticketId, new CreateCommentRequest("  Hello  "), userId);

        Assert.False(result.IsError);
        Assert.Equal("Hello", result.Value.Body);
        Assert.Equal("Test User", result.Value.AuthorName);
    }

    [Fact]
    public async Task Add_Empty_Comment_Returns_Validation()
    {
        using var db = TestSupport.NewDb();
        var (ticketId, userId, _) = await Seed(db);
        var comments = TestSupport.NewCommentService(db);

        var result = await comments.AddAsync(ticketId, new CreateCommentRequest("   "), userId);

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.Validation, result.FirstError.Type);
    }

    [Fact]
    public async Task Add_Comment_To_Missing_Ticket_Returns_NotFound()
    {
        using var db = TestSupport.NewDb();
        var comments = TestSupport.NewCommentService(db);

        var result = await comments.AddAsync(Guid.NewGuid(), new CreateCommentRequest("Hi"), Guid.NewGuid());

        Assert.True(result.IsError);
        Assert.Equal(ErrorType.NotFound, result.FirstError.Type);
    }

    [Fact]
    public async Task List_Returns_Comments_Oldest_First()
    {
        using var db = TestSupport.NewDb();
        var (ticketId, userId, _) = await Seed(db);
        var comments = TestSupport.NewCommentService(db);

        await comments.AddAsync(ticketId, new CreateCommentRequest("first"), userId);
        await Task.Delay(5);
        await comments.AddAsync(ticketId, new CreateCommentRequest("second"), userId);

        var list = (await comments.ListAsync(ticketId)).Value;

        Assert.Equal(2, list.Count);
        Assert.Equal("first", list[0].Body);
        Assert.Equal("second", list[1].Body);
    }

    [Fact]
    public async Task Adding_Comment_Does_Not_Change_Ticket_ModifiedAt()
    {
        using var db = TestSupport.NewDb();
        var (ticketId, userId, originalModifiedAt) = await Seed(db);
        var comments = TestSupport.NewCommentService(db);

        await Task.Delay(5);
        await comments.AddAsync(ticketId, new CreateCommentRequest("a comment"), userId);

        var ticket = await db.Tickets.FirstAsync(t => t.Id == ticketId);
        Assert.Equal(originalModifiedAt, ticket.ModifiedAt);
    }
}
