using System.ComponentModel.DataAnnotations;

namespace SimpleHttpListenerServer.Entities;

public class User
{
    public long Id { get; set; }
    //[RegularExpression(@"^[a-zA-Z0-9_]{5,20}$", ErrorMessage = "Username must be between 5 and 20 characters.")]
    public string Login { get; set; }
    //[RegularExpression(@"^(?=.*[A-Za-z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$", 
    //    ErrorMessage = "Password must be between 8 and 20 characters, at least one digit, special symbol, and upper case letter.")]
    public string Password { get; set; }
}