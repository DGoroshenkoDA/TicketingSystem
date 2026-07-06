namespace Ticketing.Services.Tickets;

public static class TicketEnums
{
    public static readonly IReadOnlySet<string> Types =
        new HashSet<string> { "bug", "feature", "fix" };

    public static readonly IReadOnlySet<string> States =
        new HashSet<string>
        {
            "new",
            "ready_for_implementation",
            "in_progress",
            "ready_for_acceptance",
            "done"
        };

    public const string DefaultState = "new";

    public static bool IsValidType(string? value) => value is not null && Types.Contains(value);
    public static bool IsValidState(string? value) => value is not null && States.Contains(value);
}
