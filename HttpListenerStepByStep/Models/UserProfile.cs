namespace HttpListenerStepByStep.Models;

public sealed class UserProfile(Guid id, string login, string displayName)
{
    public Guid Id { get; init; } = id;
    public string Login { get; init; } = login;
    public string DisplayName { get; init; } = displayName;
    public string Role { get; set; } = "User";
}