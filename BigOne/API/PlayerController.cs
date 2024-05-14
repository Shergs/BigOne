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

    [HttpGet("getplayersongs")]
    public async Task<IActionResult> GetPlayerSongs(string serverId)
    {
        //VoteLavalinkPlayer player = _audioService.Players.RetrieveAsync(ulong.Parse(serverId), 0, playerFactory: PlayerFactory.Vote); //_audioService.Players.GetPlayerAsync(ulong.Parse(serverId));
        VoteLavalinkPlayer? player = await _audioService.Players.GetPlayerAsync<VoteLavalinkPlayer>(ulong.Parse(serverId));
        if (player == null)
        {
            return NotFound("Player not found.");
        }
        
        var queue = player.Queue.Select(track => new Song
        {
            Name = track.Track.Title,
            Url = track.Track.Uri.ToString(),
        }).ToList();

        return Ok(queue);
    }

    [HttpGet("getplayerposition")]
    public async Task<IActionResult> GetPlayerHistory(string serverId)
    {
        //VoteLavalinkPlayer? player = await _audioService.Players.GetPlayerAsync<VoteLavalinkPlayer>(ulong.Parse(serverId));
        //if (player == null)
        //{
        //    return NotFound("Player not found.");
        //}

        //var queue = player.Queue.Select(track => new Song
        //{
        //    Name = track.Title,
        //    Url = track.Url,
        //    Duration = track.Duration
        //}).ToList();

        //return Ok(queue);
        return Ok();
    }
}
