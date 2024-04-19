public class Sound
{
    public Sound()
    {
        Name = "";
        FilePath = "";
        Emote = "";
    }
    public int Id { get; set; }
    public string Name { get; set; }
    public string FilePath { get; set; }
    public string Emote { get; set; }
    public string ServerId { get; set; }
}