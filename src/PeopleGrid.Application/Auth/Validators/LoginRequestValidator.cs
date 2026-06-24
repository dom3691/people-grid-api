using FluentValidation;
using PeopleGrid.Application.Auth.DTOs;

namespace PeopleGrid.Application.Auth.Validators;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.TenantCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.EmailOrUserName).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.TenantCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}

public sealed class LogoutRequestValidator : AbstractValidator<LogoutRequest>
{
    public LogoutRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}

public sealed class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.TenantCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.EmailOrUserName).NotEmpty().MaximumLength(256);
    }
}

public sealed class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.TenantCode).NotEmpty().MaximumLength(50);
        RuleFor(x => x.ResetToken).NotEmpty();
        RuleFor(x => x.NewPassword).SetValidator(new PasswordComplexityValidator());
        RuleFor(x => x.ConfirmPassword).Equal(x => x.NewPassword);
    }
}

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).SetValidator(new PasswordComplexityValidator());
        RuleFor(x => x.ConfirmPassword).Equal(x => x.NewPassword);
    }
}

public sealed class PasswordComplexityValidator : AbstractValidator<string>
{
    public PasswordComplexityValidator()
    {
        RuleFor(x => x)
            .NotEmpty()
            .MinimumLength(8)
            .Must(x => x.Any(char.IsUpper)).WithMessage("Password must contain at least one uppercase letter.")
            .Must(x => x.Any(char.IsLower)).WithMessage("Password must contain at least one lowercase letter.")
            .Must(x => x.Any(char.IsDigit)).WithMessage("Password must contain at least one number.")
            .Must(x => x.Any(ch => !char.IsLetterOrDigit(ch))).WithMessage("Password must contain at least one special character.");
    }
}
