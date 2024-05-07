using Discord.WebSocket;
using Lavalink4NET;
using Microsoft.EntityFrameworkCore.Query;

public class AudioManager : IHostedService
{
    private readonly AudioService _audioService;
    private readonly IEnumerable<DiscordSocketClient> _clients;
    private int _readyClients = 0;
    private bool _audioRunning;

    public AudioManager(AudioService audioService, IEnumerable<DiscordSocketClient> clients)
    {
        _audioService = audioService;
        _clients = clients;
        _audioRunning = false;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var client in _clients)
        {
            client.Ready += OnClientReady;
        }
        return Task.CompletedTask;
    }

    private async Task OnClientReady()
    {
        int readyCount = Interlocked.Increment(ref _readyClients);
        if (readyCount == _clients.Count())
        {
            if (!_audioRunning)
            {
                _audioRunning = true;
                await _audioService.StartAsync();
            }
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var client in _clients)
        {
            client.Ready -= OnClientReady;
        }
        await _audioService.StopAsync();
        _audioRunning = false;
    }
}