using Discord;

namespace BigOne.Services
{
    public interface IEmbedService
    {
        Embed GetErrorEmbed(string message);
        Embed GetPlayerEmbed(string title, string uri, string author, string source);
    }
    public class EmbedService(
        ) : IEmbedService
    {
        public Embed GetErrorEmbed(string message)
        {
            return new EmbedBuilder()
                .WithColor(Color.Red)
                .WithDescription(message)
                .Build();
        }

        public Embed GetPlayerEmbed(string title, string uri, string author, string source, bool addToQueue = false)
        {
            return new EmbedBuilder()
                            .WithColor(Color.Blue)
                            .WithDescription(!addToQueue ? $"🔈Now playing: [{title}]({uri})\n" : $"🔈Added to Queue: [{title}]({uri})" +
                                $"Link: {uri}") // Make the title a clickable link
                            .AddField("Artist", author, inline: true)
                            .AddField("Source", source, inline: true)
                            .WithFooter(footer => footer.Text = "Play some more songs.")
                            .WithCurrentTimestamp()
                            .Build();
        }
    }
}
