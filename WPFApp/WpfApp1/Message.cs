public class Message
{
    public string Author { get; set; }
    public string Body { get; set; }
    public bool IsLocal { get; set; }

    public Message(string author, string body, bool isLocal = false)
    {
        Author = author;
        Body = body;
        IsLocal = isLocal;
    }
}
