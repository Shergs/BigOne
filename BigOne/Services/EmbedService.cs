using Discord;
using System.Text;

namespace BigOne.Services
{
    public interface IEmbedService
    {
        Embed GetErrorEmbed(string message);
        (Embed, MessageComponent) GetPlayerEmbedWithButtons(string title, string uri, string author, string source, bool addToQueue = false);
        (Embed, MessageComponent) GetSoundboardWithButtons(List<Sound> sounds, string serverId);
        Embed GetSoundboard(List<Sound> sounds, string serverId);
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

        //public (string, MessageComponent) GetSoundboardWithButtons(List<Sound> sounds, string serverId)
        //{
        //    string result = "";
        //    for (int i = 0; i < sounds.Count; i++)
        //    {
        //        result += $"{i}. {sounds[i].Emote}{sounds[i].Name}\n";
        //    }
        //    return (result, CreateSoundboardControls(sounds, serverId));
        //}

        public (Embed, MessageComponent) GetSoundboardWithButtons(List<Sound> sounds, string serverId)
        {
            var embedBuilder = new EmbedBuilder()
                .WithTitle("Soundboard")
                .WithColor(Color.Blue);  // You can choose any appropriate color.

            for (int i = 0; i < sounds.Count; i++)
            {
                // Adding each sound with a play button to the embed description or fields
                embedBuilder.AddField($"{sounds[i].Emote} {sounds[i].Name}", $"Press the button to play", inline: true);
            }

            //var buttons = CreateSoundboardControls(sounds, serverId);

            // for testing
            MessageComponent buttons = null;
            return (embedBuilder.Build(), buttons);
        }

        public Embed GetSoundboard(List<Sound> sounds, string serverId)
        {
            var embedBuilder = new EmbedBuilder()
                            .WithTitle("Soundboard")
                            .WithDescription("Use '/sound' with the sound's name to play!")
                            .WithColor(Color.Blue);
            for (int i = 0; i < sounds.Count; i++)
            {
                embedBuilder.AddField($"{i+1}. {sounds[i].Emote} {sounds[i].Name}", "\u200B", inline:true);
            }

            return embedBuilder.Build();
        }

        //public (Embed, MessageComponent) GetSoundboardWithButtons(List<Sound> sounds, string serverId)
        //{
        //    var embedBuilder = new EmbedBuilder()
        //        .WithTitle("Soundboard")
        //        .WithColor(Color.Blue);

        //    // Using description to list sounds can sometimes be cleaner for alignment with buttons
        //    var description = new StringBuilder("Select a sound to play:\n");
        //    for (int i = 0; i < sounds.Count; i++)
        //    {
        //        description.AppendLine($"{i + 1}. {sounds[i].Emote} {sounds[i].Name}");
        //    }
        //    embedBuilder.WithDescription(description.ToString());

        //    var buttons = CreateSoundboardControls(sounds, serverId);

        //    return (embedBuilder.Build(), buttons);
        //}

        public Embed GetPlayerEmbed(string title, string uri, string author, string source, bool addToQueue = false)
        {
            return new EmbedBuilder()
                        .WithColor(Color.Blue)
                        .WithDescription(!addToQueue ? $"🔈Now playing: [{title}]({uri})\n" : $"🔈Added to Queue: [{title}]({uri})")
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

        //private MessageComponent CreateSoundboardControls(List<Sound> sounds, string serverId)
        //{
        //    var buttons = new ComponentBuilder()
        //        .WithButton("Next", "next_button", ButtonStyle.Success)
        //        .WithButton("Play", "play_button", ButtonStyle.Primary)
        //        .WithButton("Pause", "pause_button", ButtonStyle.Secondary)
        //        .Build();

        //    return buttons;
        //}
        //private MessageComponent CreateSoundboardControls(List<Sound> sounds, string serverId)
        //{
        //    var componentBuilder = new ComponentBuilder();

        //    for (int i = 0; i < sounds.Count; i++)
        //    {
        //        // Ensure each button has a unique ID that also includes the sound index or identifier
        //        componentBuilder.WithButton("Play", $"play_button_{i}", ButtonStyle.Primary, row: i / 5);  // Adjust row based on your layout preference
        //    }

        //    return componentBuilder.Build();
        //}

        //private MessageComponent CreateSoundboardControls(List<Sound> sounds, string serverId)
        //{
        //    var componentBuilder = new ComponentBuilder();
        //    for (int i = 0; i < sounds.Count; i++)
        //    {
        //        // Adding a button for each sound with a custom ID that includes the index
        //        componentBuilder.WithButton($"{sounds[i].Name}", $"play_button_{i}", ButtonStyle.Primary, row: i / 5);
        //    }

        //    return componentBuilder.Build();
        //}

        public class EmbedMessage
        {
            public Embed? Embed;
            public MessageComponent? MessageComponent;
        }
    }
}
