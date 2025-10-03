namespace SimpleHttpListenerServer.Models;

public class BulkElementsRequest
{
    public List<ElementDto> Elements { get; set; } = new();
}