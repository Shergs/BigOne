namespace BigOne.API;
using Microsoft.AspNetCore.Mvc;
using Lavalink4NET;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Players.Vote;
using System.Text.Json;
using BigOne.Services;
using Discord.WebSocket;

[ApiController]
[Route("[controller]")]
public class PlayerController : ControllerBase
{
    //Going to make a player service and make that a dependency here instead.
    private readonly IAudioService _audioService;
    private readonly ISignalService _signalService;
    private readonly IPlayerService _playerService;
    private readonly ISoundService _soundService;
    //private readonly DiscordSocketClient _discordSocketClient;

    public PlayerController(IAudioService audioService, ISignalService signalService, IPlayerService playerService, ISoundService soundService)
    {
        _audioService = audioService;
        _signalService = signalService;
        _playerService = playerService;
        _soundService = soundService;
    }

    [HttpGet("getplayersong")]
    public async Task<IActionResult> GetPlayerSong([FromQuery] string serverId)
    {
        VoteLavalinkPlayer? player = await _audioService.Players.GetPlayerAsync<VoteLavalinkPlayer>(ulong.Parse(serverId));
        if (player == null)
        {
            return Ok(JsonSerializer.Serialize(""));
        }

        if (player.CurrentTrack == null)
        {
            return Ok(JsonSerializer.Serialize(""));
        }
        var track = new Song
        {
            Name = player.CurrentTrack.Title,
            Url = player.CurrentTrack.Uri.ToString(),
            Artist = player.CurrentTrack.Author
        };

        return Ok(track);
    }

    [HttpGet("getplayersongs")]
    public async Task<IActionResult> GetPlayerSongs([FromQuery] string serverId)
    {
        VoteLavalinkPlayer? player = await _audioService.Players.GetPlayerAsync<VoteLavalinkPlayer>(ulong.Parse(serverId));
        if (player == null)
        {
            return Ok(JsonSerializer.Serialize(""));
        }

        var queue = player.Queue.Select(track => new Song
        {
            Name = track.Track.Title,
            Url = track.Track.Uri.ToString(),
            Artist = track.Track.Author,
            QueuePosition = player.Queue.IndexOf(track)
        }).ToList();

        return Ok(queue);
    }

    [HttpGet("getplayerposition")]
    public async Task<IActionResult> GetPlayerPosition(string serverId)
    {
        VoteLavalinkPlayer? player = await _audioService.Players.GetPlayerAsync<VoteLavalinkPlayer>(ulong.Parse(serverId));
        if (player == null)
        {
            return Ok(JsonSerializer.Serialize(""));
        }
        var position = player.Position?.Position;
        return Ok((int)Math.Floor((decimal)position?.TotalSeconds));
    }

    [HttpPost("resume")]
    public async Task<IActionResult> Resume([FromQuery] string serverId, [FromQuery] string username)
    {
        VoteLavalinkPlayer? player = await _audioService.Players.GetPlayerAsync<VoteLavalinkPlayer>(ulong.Parse(serverId));
        if (player == null)
        {
            return Ok(JsonSerializer.Serialize(""));
        }
        await _playerService.ResumeAsync(serverId, username, player.VoiceChannelId.ToString());
        return Ok();
    }

    [HttpPost("pause")]
    public async Task<IActionResult> Pause([FromQuery] string serverId, [FromQuery] string username)
    {
        VoteLavalinkPlayer? player = await _audioService.Players.GetPlayerAsync<VoteLavalinkPlayer>(ulong.Parse(serverId));
        if (player == null)
        {
            return Ok(JsonSerializer.Serialize(""));
        }
        await _playerService.PauseAsync(serverId, username, player.VoiceChannelId.ToString());
        return Ok();
    }

    [HttpPost("skip")]
    public async Task<IActionResult> Skip([FromQuery] string serverId, [FromQuery] string username)
    {
        VoteLavalinkPlayer? player = await _audioService.Players.GetPlayerAsync<VoteLavalinkPlayer>(ulong.Parse(serverId));
        if (player == null)
        {
            return Ok(JsonSerializer.Serialize(""));
        }
        await _playerService.SkipAsync(serverId, username, player.VoiceChannelId.ToString());
        return Ok();
    }

    [HttpPost("seek")]
    public async Task<IActionResult> SetPosition([FromQuery] string serverId, [FromQuery] int seconds, [FromQuery] string username)
    {
        VoteLavalinkPlayer? player = await _audioService.Players.GetPlayerAsync<VoteLavalinkPlayer>(ulong.Parse(serverId));
        if (player == null)
        {
            return Ok(JsonSerializer.Serialize(""));
        }

        TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);

        await player.SeekAsync(timeSpan).ConfigureAwait(false);
        await _signalService.SendSeekVideo(serverId, username, seconds.ToString());

        return Ok();
    }

    [HttpPost("moveupinqueue")]
    public async Task<IActionResult> MoveUpInQueue([FromQuery] string serverId, [FromQuery] int index, [FromQuery] string username)
    {
        VoteLavalinkPlayer? player = await _audioService.Players.GetPlayerAsync<VoteLavalinkPlayer>(ulong.Parse(serverId));
        if (player == null)
        {
            return Ok(JsonSerializer.Serialize(""));
        }

        var queue = player.Queue.ToList(); // Convert the queue to a list for easier manipulation.

        // Check if the index is within the range and not the first item already.
        if (index < 1 || index >= queue.Count)
        {
            return Ok(JsonSerializer.Serialize(""));
        }

        // Swap the tracks.
        var itemToMoveUp = queue[index];
        queue[index] = queue[index - 1];
        queue[index - 1] = itemToMoveUp;

        // Clear the current queue and enqueue the modified list.
        await player.Queue.RemoveAllAsync(item => true);
        await player.Queue.AddRangeAsync(queue);   

        await _signalService.SendMoveUpInQueue(serverId, username, index.ToString());
        
        return Ok();
    }

    [HttpPost("movedowninqueue")]
    public async Task<IActionResult> MoveDownInQueue([FromQuery] string serverId, [FromQuery] int index, [FromQuery] string username)
    {
        VoteLavalinkPlayer? player = await _audioService.Players.GetPlayerAsync<VoteLavalinkPlayer>(ulong.Parse(serverId));
        if (player == null)
        {
            return Ok(JsonSerializer.Serialize(""));
        }

        var queue = player.Queue.ToList(); // Convert the queue to a list for easier manipulation.

        // Check if the index is within the range and not the first item already.
        if (index < 1 || index + 1 >= queue.Count)
        {
            return Ok(JsonSerializer.Serialize(""));
        }

        // Swap the tracks.
        var itemToMoveDown = queue[index];
        queue[index] = queue[index + 1];
        queue[index + 1] = itemToMoveDown;

        // Clear the current queue and enqueue the modified list.
        await player.Queue.RemoveAllAsync(item => true);
        await player.Queue.AddRangeAsync(queue);

        await _signalService.SendMoveDownInQueue(serverId, username, index.ToString());

        return Ok();
    }


    [HttpPost("deletefromqueue")]
    public async Task<IActionResult> DeleteFromQueue([FromQuery] string serverId, [FromQuery] int index, [FromQuery] string username)
    {
        VoteLavalinkPlayer? player = await _audioService.Players.GetPlayerAsync<VoteLavalinkPlayer>(ulong.Parse(serverId));
        if (player == null)
        {
            return Ok(JsonSerializer.Serialize(""));
        }

        await player.Queue.RemoveAtAsync(index);
        await _signalService.SendDeleteFromQueue(serverId, username, index.ToString());

        return Ok();
    }

    [HttpPost("playsound")]
    public async Task<IActionResult> PlaySound([FromQuery] string serverId, [FromQuery] string soundName, [FromQuery] string voiceChannelId, [FromQuery] string username)
    {
        await _soundService.PlaySoundAsync(serverId, soundName, voiceChannelId, username);
        return Ok();
    }

    [HttpPost("playsong")]
    public async Task<IActionResult> PlaySong([FromQuery] string serverId, [FromQuery] string username, [FromQuery] string queryString, [FromQuery] string voiceChannelId)
    {
        await _playerService.PlayAsync(serverId, queryString, username, voiceChannelId);
        return Ok();
    }
}
