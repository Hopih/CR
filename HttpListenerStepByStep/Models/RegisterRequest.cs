namespace HttpListenerStepByStep.Models;

public sealed class RegisterRequest(string? login, string? displayName, string? password)
{
    public string? Login { get; set; } = login;
    public string? DisplayName { get; set; } = displayName;
    public string? Password { get; set; } = password;
}