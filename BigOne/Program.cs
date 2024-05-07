using Discord.Interactions;
using Discord.WebSocket;
using Lavalink4NET.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenAI_API;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using BigOne.Modules.GeneralBotModules;
using Google.Api;
using Lavalink4NET;
using BigOne.BotHosts;
using BigOne;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<KeyedSingletonRegistry>();

// Discord
// Configuration for GeneralBot
builder.Services.AddSingleton<DiscordSocketClient>(serviceProvider =>
{
    var registry = serviceProvider.GetRequiredService<KeyedSingletonRegistry>();
    return registry.GetOrCreate("GeneralBotSocketClient", () => new DiscordSocketClient());
});

builder.Services.AddSingleton<InteractionService>(serviceProvider =>
{
    var registry = serviceProvider.GetRequiredService<KeyedSingletonRegistry>();
    var client = serviceProvider.GetRequiredService<DiscordSocketClient>(); // Assuming client is resolved the same way
    return registry.GetOrCreate("GeneralBotInteractions", () => new InteractionService(client));
});

// Configuration for SoundBot
builder.Services.AddSingleton<DiscordSocketClient>(serviceProvider =>
{
    var registry = serviceProvider.GetRequiredService<KeyedSingletonRegistry>();
    return registry.GetOrCreate("SoundBotSocketClient", () => new DiscordSocketClient());
});
builder.Services.AddSingleton<InteractionService>(serviceProvider =>
{
    var registry = serviceProvider.GetRequiredService<KeyedSingletonRegistry>();
    var client = serviceProvider.GetRequiredService<DiscordSocketClient>(); // Resolve specifically for SoundBot
    return registry.GetOrCreate("SoundBotInteractions", () => new InteractionService(client));
});

builder.Services.AddSingleton<ConversationService>();
builder.Services.AddHostedService<DiscordClientHost>();
builder.Services.AddHostedService<DiscordSoundBot>();

// Lavalink
builder.Services.ConfigureLavalink(config =>
    config.ReadyTimeout = TimeSpan.FromSeconds(60)
);
builder.Services.AddLavalink();
builder.Services.AddLogging(x => x.AddConsole().SetMinimumLevel(LogLevel.Trace));

//Database
var connectionString = builder.Configuration.GetConnectionString("BigOne") ?? throw new InvalidOperationException("Connection string 'BigOne' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString),
    ServiceLifetime.Singleton);

// Add controllers for API
builder.Services.AddControllers();

// Logging
builder.Services.AddLogging(x => x.AddConsole().SetMinimumLevel(LogLevel.Trace));

// Build the application
var app = builder.Build();

// Configure the HTTP request pipeline
//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Error");
//    app.UseHsts();
//}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

// Map controllers
app.MapControllers();

// Run the application
app.Run();





