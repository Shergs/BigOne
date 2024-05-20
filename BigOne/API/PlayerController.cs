﻿namespace BigOne.API;
using Microsoft.AspNetCore.Mvc;
using Lavalink4NET;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Queued;
using Lavalink4NET.Players.Vote;
using System.Text.Json;

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
            Artist = track.Track.Author
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
    public async Task<IActionResult> Resume([FromQuery] string serverId)
    {
        VoteLavalinkPlayer? player = await _audioService.Players.GetPlayerAsync<VoteLavalinkPlayer>(ulong.Parse(serverId));
        if (player == null)
        {
            return Ok(JsonSerializer.Serialize(""));
        }

        await player.ResumeAsync();
    }

    [HttpPost("pause")]
    public async Task<IActionResult> Pause([FromQuery] string serverId)
    {
        VoteLavalinkPlayer? player = await _audioService.Players.GetPlayerAsync<VoteLavalinkPlayer>(ulong.Parse(serverId));
        if (player == null)
        {
            return Ok(JsonSerializer.Serialize(""));
        }

        await player.PauseAsync().ConfigureAwait(false);
        return Ok();
    }

    [HttpPost("skip")]
    public async Task<IActionResult> Skip([FromQuery] string serverId)
    {
        VoteLavalinkPlayer? player = await _audioService.Players.GetPlayerAsync<VoteLavalinkPlayer>(ulong.Parse(serverId));
        if (player == null)
        {
            return Ok(JsonSerializer.Serialize(""));
        }

        await player.SkipAsync().ConfigureAwait(false);
        return Ok();
    }

    [HttpPost("position")]
    public async Task<IActionResult> SetPosition([FromQuery] string serverId, [FromQuery] int seconds)
    {
        VoteLavalinkPlayer? player = await _audioService.Players.GetPlayerAsync<VoteLavalinkPlayer>(ulong.Parse(serverId));
        if (player == null)
        {
            return Ok(JsonSerializer.Serialize(""));
        }

        TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);

        await player.SeekAsync(timeSpan).ConfigureAwait(false);
        return Ok();
    }
}
