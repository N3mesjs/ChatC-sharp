public class Message
{
    public string Author { get; set; }
    public string Body { get; set; }

    public Message(string author, string body)
    {
        Author = author;
        Body = body;
    }
}
