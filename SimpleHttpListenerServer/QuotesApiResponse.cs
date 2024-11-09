namespace SimpleHttpListenerServer;

public class QuotesApiResponse
{
    public int Id { get; set; }
    public Originator Originator { get; set; }
    public string LanguageCode { get; set; }
    public string Content { get; set; }
    public string Url { get; set; }
    public List<string> Tags { get; set; }
}

public class Originator
{
    public int Id { get; set; }
    public string LanguageCode { get; set; }
    public string Description { get; set; }
    public int MasterId { get; set; }
    public string Name { get; set; }
    public string Url { get; set; }
}