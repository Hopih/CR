using System.ComponentModel.DataAnnotations;

namespace SimpleHttpListenerServer.Entities;

public class User
{
    public int Id { get; set; }
    //[RegularExpression(@"^[a-zA-Z0-9_]{5,20}$", ErrorMessage = "Username must be between 5 and 20 characters.")]
    public string? Login { get; set; }
   
    public string? Password { get; set; }
    public string? Role { get; set; }
}