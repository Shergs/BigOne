namespace BigOne.API;
using Microsoft.AspNetCore.Mvc;
using Lavalink4NET;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Players.Vote;

[ApiController]
[Route("[controller]")]
public class PlayerController : ControllerBase
{
    private readonly IAudioService _audioService;

    public PlayerController(IAudioService audioService)
    {
        _audioService = audioService;
    }

    [HttpGet("getplayersong")]
    public async Task<IActionResult> GetPlayerSong([FromQuery] string serverId)
    {
        VoteLavalinkPlayer? player = await _audioService.Players.GetPlayerAsync<VoteLavalinkPlayer>(ulong.Parse(serverId));
        if (player == null)
        {
            return Ok("");
        }

        var track = new Song
        {
            Name = player.CurrentTrack.Title,
            Url = player.CurrentTrack.Uri.ToString()
        };

        return Ok(track);
    }

    [HttpGet("getplayersongs")]
    public async Task<IActionResult> GetPlayerSongs([FromQuery] string serverId)
    {
        VoteLavalinkPlayer? player = await _audioService.Players.GetPlayerAsync<VoteLavalinkPlayer>(ulong.Parse(serverId));
        if (player == null)
        {
            return Ok("");
        }
        
        var queue = player.Queue.Select(track => new Song
        {
            Name = track.Track.Title,
            Url = track.Track.Uri.ToString(),
        }).ToList();

        return Ok(queue);
    }

    [HttpGet("getplayerposition")]
    public async Task<IActionResult> GetPlayerPosition(string serverId)
    {
        VoteLavalinkPlayer? player = await _audioService.Players.GetPlayerAsync<VoteLavalinkPlayer>(ulong.Parse(serverId));
        if (player == null)
        {
            return Ok("");
        }

        var position = player.Position?.Position;
        return Ok(position.ToString());
    }
}
