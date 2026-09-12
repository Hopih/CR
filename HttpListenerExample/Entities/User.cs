using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HttpListenerExample.Entities;

public class User
{
    public long Id { get; set; }
    public required string Email { get; set; }

    public required string Password { get; set; }

    public required string FullName { get; set; }

    public List<News> News { get; set; } = [];
}