namespace BigOne.Modules;

using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Lavalink4NET;
using Lavalink4NET.DiscordNet;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Vote;
using Lavalink4NET.Rest.Entities.Tracks;
using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Models;
using OpenAI_API.Moderation;

/// <summary>
///     Presents some of the main features of the Lavalink4NET-Library.
/// </summary>
[RequireContext(ContextType.Guild)]
public sealed class RandomModule : InteractionModuleBase<SocketInteractionContext>
{
    public RandomModule()
    {

    }

    [SlashCommand("peter", description: "Insult Peter", runMode: RunMode.Async)]
    public async Task Peter()
    {
        await RespondAsync("Peter functionality has been discontinued for the forseeable future out of my never ending love for Peter. Sorry Peter 💕💕💕").ConfigureAwait(false);
    }
}
