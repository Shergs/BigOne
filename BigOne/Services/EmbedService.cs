using Discord;

namespace BigOne.Services
{
    public interface IEmbedService
    {
        Embed GetErrorEmbed(string message);
        (Embed, MessageComponent) GetPlayerEmbedWithButtons(string title, string uri, string author, string source, bool addToQueue = false);
        (string, MessageComponent) GetSoundboardWithButtons(List<Sound> sounds, string serverId);
        Embed GetPlayerEmbed(string title, string uri, string author, string source, bool addToQueue = false);
        Embed GetMessageEmbed(string title, string message);
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

        public (Embed, MessageComponent) GetPlayerEmbedWithButtons(string title, string uri, string author, string source, bool addToQueue = false)
        {
            var embed = new EmbedBuilder()
                            .WithColor(Color.Blue)
                            .WithDescription(!addToQueue ? $"🔈Now playing: [{title}]({uri})\n" : $"🔈Added to Queue: [{title}]({uri})" +
                                $"Link: {uri}") // Make the title a clickable link
                            .AddField("Artist", author, inline: true)
                            .AddField("Source", source, inline: true)
                            .WithFooter(footer => footer.Text = "Play some more songs.")
                            .WithCurrentTimestamp()
                            .Build();

            var components = new ComponentBuilder()
                .WithButton("Play", "play_button", ButtonStyle.Primary)
                .WithButton("Pause", "pause_button", ButtonStyle.Secondary)
                .WithButton("Next", "next_button", ButtonStyle.Success)
                .WithButton("Previous", "previous_button", ButtonStyle.Danger)
                .Build();

            return (embed, CreatePlayerControls());
        }

        public (string, MessageComponent) GetSoundboardWithButtons(List<Sound> sounds, string serverId)
        {
            string result = "";
            for (int i = 0; i < sounds.Count; i++)
            {
                result += $"{i}. {sounds[i].Emote}{sounds[i].Name}\n";
            }
            return (result, CreateSoundboardControls(sounds, serverId));
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

        public Embed GetMessageEmbed(string title, string message)
        {
            return new EmbedBuilder()
                            .WithColor(Color.Blue)
                            .WithDescription(message)
                            .Build();
        }

        private MessageComponent CreatePlayerControls()
        {
            var buttons = new ComponentBuilder()
                .WithButton("Next", "next_button", ButtonStyle.Success)
                .WithButton("Play", "play_button", ButtonStyle.Primary)
                .WithButton("Pause", "pause_button", ButtonStyle.Secondary)
                .Build();

            return buttons;
        }

        private MessageComponent CreateSoundboardControls(List<Sound> sounds, string serverId)
        {
            var buttons = new ComponentBuilder()
                .WithButton("Next", "next_button", ButtonStyle.Success)
                .WithButton("Play", "play_button", ButtonStyle.Primary)
                .WithButton("Pause", "pause_button", ButtonStyle.Secondary)
                .Build();

            return buttons;
        }

        public class EmbedMessage
        {
            public Embed? Embed;
            public MessageComponent? MessageComponent;
        }
    }
}
