namespace SimpleHttpListenerServer.Models;

public sealed class SignupRequest
{
    public string Login { get; set; } = ""; 
    public string Password { get; set; } = "";
}