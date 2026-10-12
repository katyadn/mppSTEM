using Microclimate.Domain.Common;
using Microclimate.Domain.Enums;
using Microclimate.Domain.Users;

namespace Microclimate.Domain.Tests.Users;

public class UserTests
{
    [Fact]
    public void Create_MixedCaseEmail_NormalizesToLowerCase()
    {
        var user = User.Create("  Operator@Example.COM ", "Оператор", "hash", UserRole.Operator, TestData.Now);

        Assert.Equal("operator@example.com", user.Email);
        Assert.True(user.IsActive);
        Assert.Equal(UserRole.Operator, user.Role);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("user@")]
    [InlineData("Ім'я <user@example.com>")]
    [InlineData("")]
    public void Create_InvalidEmail_Throws(string email)
    {
        Assert.Throws<DomainException>(() => User.Create(email, "User", "hash", UserRole.Viewer, TestData.Now));
    }

    [Fact]
    public void Create_UndefinedRole_Throws()
    {
        Assert.Throws<DomainException>(() => User.Create("a@b.com", "User", "hash", (UserRole)0, TestData.Now));
    }

    [Fact]
    public void ChangeRole_Deactivate_ChangePassword_Rename_UpdateState()
    {
        var user = User.Create("a@b.com", "User", "hash", UserRole.Viewer, TestData.Now);

        user.ChangeRole(UserRole.Admin);
        user.ChangePasswordHash("new-hash");
        user.Rename("Адмін");
        user.Deactivate();

        Assert.Equal(UserRole.Admin, user.Role);
        Assert.Equal("new-hash", user.PasswordHash);
        Assert.Equal("Адмін", user.DisplayName);
        Assert.False(user.IsActive);

        user.Activate();
        Assert.True(user.IsActive);
    }

    [Fact]
    public void RefreshToken_Issue_IsActiveUntilExpiry()
    {
        var token = RefreshToken.Issue(TestData.UserId, "token-hash", TestData.Now, TimeSpan.FromDays(7));

        Assert.Equal(TestData.Now.AddDays(7), token.ExpiresAt);
        Assert.True(token.IsActive(TestData.Now.AddDays(7).AddTicks(-1)));
        Assert.False(token.IsActive(TestData.Now.AddDays(7)));
    }

    [Fact]
    public void RefreshToken_Revoke_WithReplacement_IsInactive()
    {
        var token = RefreshToken.Issue(TestData.UserId, "old", TestData.Now, TimeSpan.FromDays(7));

        token.Revoke(TestData.Now.AddHours(1), "new");

        Assert.False(token.IsActive(TestData.Now.AddHours(2)));
        Assert.Equal("new", token.ReplacedByTokenHash);
        Assert.Equal(TestData.Now.AddHours(1), token.RevokedAt);
    }

    [Fact]
    public void RefreshToken_RevokeTwice_Throws()
    {
        var token = RefreshToken.Issue(TestData.UserId, "old", TestData.Now, TimeSpan.FromDays(7));
        token.Revoke(TestData.Now);

        Assert.Throws<DomainException>(() => token.Revoke(TestData.Now));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RefreshToken_NonPositiveLifetime_Throws(int days)
    {
        Assert.Throws<DomainException>(() =>
            RefreshToken.Issue(TestData.UserId, "hash", TestData.Now, TimeSpan.FromDays(days)));
    }

    [Fact]
    public void RefreshToken_EmptyUser_Throws()
    {
        Assert.Throws<DomainException>(() =>
            RefreshToken.Issue(Guid.Empty, "hash", TestData.Now, TimeSpan.FromDays(1)));
    }
}
