using FluentValidation;
using PeopleGrid.Application.Auth.DTOs;

namespace PeopleGrid.Application.Auth.Validators;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.TenantCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.EmailOrUserName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(6);
    }
}
