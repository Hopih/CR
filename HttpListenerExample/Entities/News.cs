namespace HttpListenerExample.Entities;

public class News
{
    public Guid Id { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }

    public long AuthorId { get; set; }
}