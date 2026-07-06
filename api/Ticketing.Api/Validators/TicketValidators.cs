using FluentValidation;
using Ticketing.Services.Tickets;

namespace Ticketing.Api.Validators;

public class CreateTicketRequestValidator : AbstractValidator<CreateTicketRequest>
{
    public CreateTicketRequestValidator()
    {
        RuleFor(x => x.TeamId).NotEmpty().WithMessage("Team is required.");
        RuleFor(x => x.Type)
            .Must(TicketEnums.IsValidType)
            .WithMessage("Type must be one of: bug, feature, fix.");
        RuleFor(x => x.Title)
            .Must(s => !string.IsNullOrWhiteSpace(s))
            .WithMessage("Title is required.");
        RuleFor(x => x.Body)
            .Must(s => !string.IsNullOrWhiteSpace(s))
            .WithMessage("Body is required.");
    }
}

public class UpdateTicketRequestValidator : AbstractValidator<UpdateTicketRequest>
{
    public UpdateTicketRequestValidator()
    {
        RuleFor(x => x.TeamId).NotEmpty().WithMessage("Team is required.");
        RuleFor(x => x.Type)
            .Must(TicketEnums.IsValidType)
            .WithMessage("Type must be one of: bug, feature, fix.");
        RuleFor(x => x.State)
            .Must(TicketEnums.IsValidState)
            .WithMessage("State is not a valid workflow state.");
        RuleFor(x => x.Title)
            .Must(s => !string.IsNullOrWhiteSpace(s))
            .WithMessage("Title is required.");
        RuleFor(x => x.Body)
            .Must(s => !string.IsNullOrWhiteSpace(s))
            .WithMessage("Body is required.");
    }
}

public class UpdateTicketStateRequestValidator : AbstractValidator<UpdateTicketStateRequest>
{
    public UpdateTicketStateRequestValidator()
    {
        RuleFor(x => x.State)
            .Must(TicketEnums.IsValidState)
            .WithMessage("State is not a valid workflow state.");
    }
}
