namespace HttpListenerStepByStep.Models;

public sealed record User(Guid Id, string Login, string DisplayName,
    string PasswordHash, string Role)
{
    public object Public() => new UserProfile(Id, Login, DisplayName)
    {
        Role = Role
    };
}