using FluentValidation;
using Ticketing.Services.Epics;

namespace Ticketing.Api.Validators;

public class CreateEpicRequestValidator : AbstractValidator<CreateEpicRequest>
{
    public CreateEpicRequestValidator()
    {
        RuleFor(x => x.TeamId).NotEmpty().WithMessage("Team is required.");
        RuleFor(x => x.Title)
            .Must(s => !string.IsNullOrWhiteSpace(s))
            .WithMessage("Epic title is required.");
    }
}

public class UpdateEpicRequestValidator : AbstractValidator<UpdateEpicRequest>
{
    public UpdateEpicRequestValidator()
    {
        RuleFor(x => x.Title)
            .Must(s => !string.IsNullOrWhiteSpace(s))
            .WithMessage("Epic title is required.");
    }
}
