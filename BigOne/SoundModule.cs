using Discord;
using Discord.Audio;
using Discord.Interactions;
using System.Diagnostics;
namespace BigOne;

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Lavalink4NET;
using Lavalink4NET.DiscordNet;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Vote;
using Lavalink4NET.Rest;
using Lavalink4NET.Rest.Entities.Tracks;
using Lavalink4NET.Tracks;
using Newtonsoft.Json;
using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Models;
using OpenAI_API.Moderation;

using System.Threading.Tasks;

[RequireContext(ContextType.Guild)]
public sealed class SoundModule : InteractionModuleBase<SocketInteractionContext>
{
    public SoundModule()
    { }
}
