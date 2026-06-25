using FluentValidation;
using PeopleGrid.Application.Features.Notifications.DTOs;

namespace PeopleGrid.Application.Features.Notifications.Validators;

public sealed class UpdateNotificationTemplateRequestValidator : AbstractValidator<UpdateNotificationTemplateRequest>
{
    public UpdateNotificationTemplateRequestValidator()
    {
        RuleFor(x => x.Subject).MaximumLength(200);
        RuleFor(x => x.Body).NotEmpty();
    }
}

public sealed class SendTestNotificationRequestValidator : AbstractValidator<SendTestNotificationRequest>
{
    public SendTestNotificationRequestValidator()
    {
        RuleFor(x => x.RecipientUserId).NotEmpty();
        RuleFor(x => x.TemplateKey).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Channel).NotEmpty().Must(x => new[] { "InApp", "Email" }.Contains(x)).WithMessage("Channel is invalid.");
    }
}
