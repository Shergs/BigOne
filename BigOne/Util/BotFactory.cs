using Discord.Interactions;
using Discord.WebSocket;

public interface IBotFactory
{
    DiscordSocketClient GetClient(string name);
    InteractionService GetInteractionService(string name);
}

public class BotFactory : IBotFactory
{
    private readonly Dictionary<string, DiscordSocketClient> _clients = new();
    private readonly Dictionary<string, InteractionService> _interactionServices = new();
    private readonly IServiceProvider _serviceProvider;

    public BotFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public DiscordSocketClient GetClient(string name)
    {
        if (!_clients.ContainsKey(name))
        {
            var client = new DiscordSocketClient();
            _clients[name] = client;
        }
        return _clients[name];
    }

    public InteractionService GetInteractionService(string name)
    {
        if (!_interactionServices.ContainsKey(name))
        {
            var services = new InteractionService(GetClient(name));
            _interactionServices[name] = services;
        }
        return _interactionServices[name];
    }
}
