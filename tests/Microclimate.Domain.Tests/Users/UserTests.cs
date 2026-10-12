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
}
