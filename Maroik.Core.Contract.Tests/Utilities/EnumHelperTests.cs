using Maroik.Core.Contract.Utilities;

namespace Maroik.Core.Contract.Tests.Utilities;

/// <summary>
/// Unit tests for <see cref="Maroik.Core.Contract.Utilities.EnumHelper"/>.
/// Verifies that <c>GetDescription</c> returns the value of the
/// <see cref="System.ComponentModel.DescriptionAttribute"/> on each enum member,
/// and falls back to the member name when no attribute is present.
/// </summary>
public class EnumHelperTests
{
    [Theory]
    [InlineData(AccountMessage.Success, "Success")]
    [InlineData(AccountMessage.MailSent, "Mail Sent")]
    [InlineData(AccountMessage.FailToMailSent, "Fail to mail sent")]
    [InlineData(AccountMessage.UserAlreadyCreated, "User already created, please login")]
    [InlineData(AccountMessage.VerifyEmail, "User already created, please verify your given mail Id")]
    [InlineData(AccountMessage.AccountLocked, "This account is locked")]
    [InlineData(AccountMessage.SuccessToResetPassword, "Success to reset password")]
    [InlineData(AccountMessage.ResetPasswordMail, "Email has been sent to reset password")]
    [InlineData(AccountMessage.InvalidUser, "Invalid User, Please Create account")]
    [InlineData(AccountMessage.InvalidToken, "Invalid Token")]
    [InlineData(AccountMessage.FailToResendEmail, "Failed to resend email")]
    [InlineData(AccountMessage.UserCreatedVerifyEmail, "User created, Check email, click link and verify")]
    [InlineData(AccountMessage.ErrorFound, "Error Found")]
    public void GetDescription_ReturnsDescriptionAttribute_ForAccountMessage(AccountMessage value, string expected)
    {
        string result = EnumHelper.GetDescription(value);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(BoardType.FreeForum, "FreeForum")]
    [InlineData(BoardType.PrivateNote, "PrivateNote")]
    public void GetDescription_ReturnsDescriptionAttribute_ForBoardType(BoardType value, string expected)
    {
        string result = EnumHelper.GetDescription(value);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(CalendarType.My, "My")]
    [InlineData(CalendarType.Other, "Other")]
    public void GetDescription_ReturnsDescriptionAttribute_ForCalendarType(CalendarType value, string expected)
    {
        string result = EnumHelper.GetDescription(value);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(Session.Account, "Account")]
    public void GetDescription_ReturnsDescriptionAttribute_ForSession(Session value, string expected)
    {
        string result = EnumHelper.GetDescription(value);

        Assert.Equal(expected, result);
    }
}
