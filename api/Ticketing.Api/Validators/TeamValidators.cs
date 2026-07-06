using FluentValidation;
using Ticketing.Services.Teams;

namespace Ticketing.Api.Validators;

public class CreateTeamRequestValidator : AbstractValidator<CreateTeamRequest>
{
    public CreateTeamRequestValidator()
    {
        RuleFor(x => x.Name)
            .Must(s => !string.IsNullOrWhiteSpace(s))
            .WithMessage("Team name is required.");
    }
}

public class UpdateTeamRequestValidator : AbstractValidator<UpdateTeamRequest>
{
    public UpdateTeamRequestValidator()
    {
        RuleFor(x => x.Name)
            .Must(s => !string.IsNullOrWhiteSpace(s))
            .WithMessage("Team name is required.");
    }
}
