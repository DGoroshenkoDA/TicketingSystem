using FluentValidation;
using Ticketing.Services.Comments;

namespace Ticketing.Api.Validators;

public class CreateCommentRequestValidator : AbstractValidator<CreateCommentRequest>
{
    public CreateCommentRequestValidator()
    {
        RuleFor(x => x.Body)
            .Must(s => !string.IsNullOrWhiteSpace(s))
            .WithMessage("Comment body is required.");
    }
}
