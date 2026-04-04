namespace Maroik.Core.Contract.Dtos;

public class AccountRequest
{
    public string? Email { get; set; }
    public string? HashedPassword { get; set; }
    public string? Nickname { get; set; }
    public string? AvatarImagePath { get; set; }
    public string? Role { get; set; }
    public string? TimeZoneIanaId { get; set; }
    public string? DefaultMonetaryUnit { get; set; }
    public bool Locked { get; set; }
    public long LoginAttempt { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool AgreedServiceTerms { get; set; }
    public string? RegistrationToken { get; set; }
    public string? ResetPasswordToken { get; set; }
    public DateTime Created { get; set; }
    public DateTime Updated { get; set; }
    public string? Message { get; set; }
    public bool Deleted { get; set; }
}