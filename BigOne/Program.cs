using Discord.Interactions;
using Discord.WebSocket;
using BigOne;
using Lavalink4NET.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenAI_API;


var builder = new HostApplicationBuilder(args);

// Discord
builder.Services.AddSingleton<DiscordSocketClient>();
builder.Services.AddSingleton<InteractionService>();
builder.Services.AddSingleton<ConversationService>();
builder.Services.AddHostedService<DiscordClientHost>();

// Lavalink
builder.Services.AddLavalink();
builder.Services.AddLogging(x => x.AddConsole().SetMinimumLevel(LogLevel.Trace));

builder.Build().Run();





