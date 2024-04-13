namespace BigOne;

using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Hosting;
using System.Configuration;

internal sealed class DiscordClientHost : IHostedService
{
    private readonly DiscordSocketClient _discordSocketClient;
    private readonly InteractionService _interactionService;
    private readonly IServiceProvider _serviceProvider;

    public DiscordClientHost(
        DiscordSocketClient discordSocketClient,
        InteractionService interactionService,
        IServiceProvider serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(discordSocketClient);
        ArgumentNullException.ThrowIfNull(interactionService);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        _discordSocketClient = discordSocketClient;
        _interactionService = interactionService;
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _discordSocketClient.InteractionCreated += InteractionCreated;
        _discordSocketClient.Ready += ClientReady;
        _discordSocketClient.MessageReceived += MessageReceivedAsync;

        // Put bot token here
        await _discordSocketClient
            .LoginAsync(TokenType.Bot, ConfigurationManager.AppSettings["DiscordAPIKey"])
            .ConfigureAwait(false);

        await _discordSocketClient
            .StartAsync()
            .ConfigureAwait(false);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _discordSocketClient.InteractionCreated -= InteractionCreated;
        _discordSocketClient.Ready -= ClientReady;

        await _discordSocketClient
            .StopAsync()
            .ConfigureAwait(false);
    }

    private Task InteractionCreated(SocketInteraction interaction)
    {
        var interactionContext = new SocketInteractionContext(_discordSocketClient, interaction);
        return _interactionService!.ExecuteCommandAsync(interactionContext, _serviceProvider);
    }

    private async Task MessageReceivedAsync(SocketMessage message)
    {
        // Ignore system messages and messages from bots
        if (message is not SocketUserMessage userMessage || message.Author.IsBot)
        {
            return;
        }

        // Minty
        if (message.Author.Id == 761069081930498130)
        {
            Random random = new Random();
            if (random.Next(1, 4) == 1)
            {
                await message.Channel.SendMessageAsync($"Shut up Minty. Minty tried to say '{message.Content}' but he is annoying so I deleted it.");
                await message.Channel.DeleteMessageAsync(message);
            }
        }

        // Me
        if (message.Author.Id == 140910636488982529)
        {
            await message.Channel.SendMessageAsync("GOAT is speaking listen up");
        }
    }

    private async Task ClientReady()
    {
        try
        {
            await _interactionService
                .AddModulesAsync(Assembly.GetExecutingAssembly(), _serviceProvider)
                .ConfigureAwait(false);

            // Register to Guild
            await _interactionService
                .RegisterCommandsToGuildAsync(783190942806835200)
                .ConfigureAwait(false);
            await _interactionService
                .RegisterCommandsToGuildAsync(1008235979045339168)
                .ConfigureAwait(false);

            Console.WriteLine("Commands registered successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error registering commands: {ex.Message}");
        }
    }
}