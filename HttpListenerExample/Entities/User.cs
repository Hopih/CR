using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HttpListenerExample.Entities;

public class User
{
    public long Id { get; set; }
    public string Email { get; set; }

    public string Password { get; set; }

    public string FullName { get; set; }

    public List<News> News { get; set; }
}