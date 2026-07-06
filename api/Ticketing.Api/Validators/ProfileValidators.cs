using FluentValidation;
using Ticketing.Services.Profile;

namespace Ticketing.Api.Validators;

public class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.DisplayName)
            .Must(s => !string.IsNullOrWhiteSpace(s))
            .WithMessage("Display name is required.");
    }
}

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().WithMessage("Current password is required.");
        RuleFor(x => x.NewPassword)
            .MinimumLength(8).WithMessage("New password must be at least 8 characters.");
    }
}
