using FluentAssertions;
using PeopleGrid.Application.Auth.DTOs;
using PeopleGrid.Application.Auth.Validators;
using PeopleGrid.Infrastructure.Security;

namespace PeopleGrid.UnitTests;

public sealed class AuthModuleTests
{
    [Fact]
    public void TokenHasher_ShouldHashTokenDeterministicallyWithoutReturningRawToken()
    {
        var token = "sample-refresh-token";

        var firstHash = TokenHasher.Hash(token);
        var secondHash = TokenHasher.Hash(token);

        firstHash.Should().Be(secondHash);
        firstHash.Should().NotBe(token);
        firstHash.Should().HaveLength(64);
    }

    [Fact]
    public void ResetPasswordValidator_ShouldRequireMatchingPasswordConfirmation()
    {
        var validator = new ResetPasswordRequestValidator();
        var request = new ResetPasswordRequest("DANGOTE", "token", "Password@123", "Different@123");

        var result = validator.Validate(request);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x => x.PropertyName == nameof(ResetPasswordRequest.ConfirmPassword));
    }
}
