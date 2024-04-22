namespace BigOne.Modules;

using System;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Lavalink4NET;
using Lavalink4NET.DiscordNet;
using Lavalink4NET.Players;
using Lavalink4NET.Players.Vote;
using Lavalink4NET.Rest.Entities.Tracks;
using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Models;
using OpenAI_API.Moderation;
using Google.Cloud.TextToSpeech.V1;
using Grpc.Net.Client;
using Google.Api.Gax;
using Google.Api.Gax.Grpc;
using Grpc.Core;
using System.IO;
using System.Text;
using System.Text.Json;
using Discord.Audio;
using System.Diagnostics;
using BigOneData.Migrations;


/// <summary>
///     Presents some of the main features of the Lavalink4NET-Library.
/// </summary>
[RequireContext(ContextType.Guild)]
public sealed class TTSModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly IConfiguration _configuration;
    private readonly ApplicationDbContext _context;

    public TTSModule(IConfiguration configuration, ApplicationDbContext context)
    {
        _configuration = configuration;
        _context = context;
    }

    [SlashCommand("speak", description: "Text to speech", runMode: RunMode.Async)]
    public async Task Speak(string query)
    {
        await DeferAsync().ConfigureAwait(false);
        Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + ";C:\\Users\\sherg\\source\\repos\\BigOne\\BigOne\\opus.dll\"");
        var voiceChannel = (Context.User as IGuildUser)?.VoiceChannel;
        IAudioClient audioClient = null;
        if (voiceChannel == null)
        {
            await ReplyAsync("You need to be in a voice channel.");
            return;
        }
        Console.WriteLine("Attempting to connect to the voice channel...");
        try
        {
            audioClient = await voiceChannel.ConnectAsync();
            Console.WriteLine("Connected to the voice channel successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to connect to voice channel: " + ex.Message);
            return;
        }

        // API key
        string apiKey = _configuration["Google:APIKey"];

        // Your Google Cloud Text-to-Speech API key
        string apiUrl = $"https://texttospeech.googleapis.com/v1/text:synthesize?key={apiKey}";

        // Create an HttpClient
        using (HttpClient client = new HttpClient())
        {
            // Setup HTTP request data
            var requestData = new
            {
                input = new { text = query },
                voice = new { languageCode = "en-US", ssmlGender = "FEMALE" },
                audioConfig = new { audioEncoding = "MP3" }
            };

            // Serialize request data to JSON
            string json = System.Text.Json.JsonSerializer.Serialize(requestData);
            HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

            // Send a POST request
            HttpResponseMessage response = await client.PostAsync(apiUrl, content);

            // Handle response
            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                using (JsonDocument doc = JsonDocument.Parse(responseContent))
                {
                    // Extract the audio content from JSON response
                    string audioContent = doc.RootElement.GetProperty("audioContent").GetString();
                    byte[] audioBytes = Convert.FromBase64String(audioContent);

                    // Write the bytes to an MP3 file
                    File.WriteAllBytes("C:\\Workspace_Git\\BigOne\\BigOne\\Sounds\\output.mp3", audioBytes);
                    Console.WriteLine("Audio content written to file 'output.mp3'");

                    await FollowupAsync("Text to speech playing.").ConfigureAwait(false);
                    using (var ffmpeg = CreateProcess("C:\\Workspace_Git\\BigOne\\BigOne\\Sounds\\output.mp3"))
                    using (var stream = audioClient.CreatePCMStream(AudioApplication.Mixed))
                    {
                        try { await ffmpeg.StandardOutput.BaseStream.CopyToAsync(stream); }
                        finally { await stream.FlushAsync(); }
                    }

                    File.Delete("C:\\Workspace_Git\\BigOne\\BigOne\\Sounds\\output.mp3");
                }
            }
            else
            {
                Console.WriteLine($"Failed to synthesize speech. Status code: {response.StatusCode}, Message: {response.ReasonPhrase}");
                await FollowupAsync("Text to speech failed");
            }
        }
    }

    [SlashCommand("speakandsavesound", description: "speak and save the sound", runMode: RunMode.Async)]
    public async Task SpeakAndSaveSound(string query, string name)
    {
        await DeferAsync().ConfigureAwait(false);
        Environment.SetEnvironmentVariable("PATH", Environment.GetEnvironmentVariable("PATH") + ";C:\\Users\\sherg\\source\\repos\\BigOne\\BigOne\\opus.dll\"");
        var voiceChannel = (Context.User as IGuildUser)?.VoiceChannel;
        IAudioClient audioClient = null;
        if (voiceChannel == null)
        {
            await ReplyAsync("You need to be in a voice channel.");
            return;
        }
        Console.WriteLine("Attempting to connect to the voice channel...");
        try
        {
            audioClient = await voiceChannel.ConnectAsync();
            Console.WriteLine("Connected to the voice channel successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to connect to voice channel: " + ex.Message);
            return;
        }

        // API key
        string apiKey = _configuration["Google:APIKey"];

        // Your Google Cloud Text-to-Speech API key
        string apiUrl = $"https://texttospeech.googleapis.com/v1/text:synthesize?key={apiKey}";

        // Create an HttpClient
        using (HttpClient client = new HttpClient())
        {
            // Setup HTTP request data
            var requestData = new
            {
                input = new { text = query },
                voice = new { languageCode = "en-US", ssmlGender = "FEMALE" },
                audioConfig = new { audioEncoding = "MP3" }
            };

            // Serialize request data to JSON
            string json = System.Text.Json.JsonSerializer.Serialize(requestData);
            HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

            // Send a POST request
            HttpResponseMessage response = await client.PostAsync(apiUrl, content);

            // Handle response
            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                using (JsonDocument doc = JsonDocument.Parse(responseContent))
                {
                    // Extract the audio content from JSON response
                    string audioContent = doc.RootElement.GetProperty("audioContent").GetString();
                    byte[] audioBytes = Convert.FromBase64String(audioContent);

                    // Write the bytes to an MP3 file
                    File.WriteAllBytes($"C:\\Workspace_Git\\BigOne\\BigOne\\Sounds\\{name.Replace(" ", "_")}.mp3", audioBytes);
                    Console.WriteLine($"Audio content written to file '{name.Replace(" ", "_")}.mp3'");

                    if (_context.Sounds.Any(x => x.Name == name && x.ServerId == Context.Guild.Id.ToString()))
                    {
                        await FollowupAsync("Sound Failed to save in database, sound of the same name and server already exists");
                    }
                    Sound sound = new Sound();
                    sound.Name = name;
                    sound.FilePath = $"{name.Replace(" ", "_")}.mp3";
                    sound.Emote = "";
                    sound.ServerId = Context.Guild.Id.ToString();
                    _context.Sounds.Add(sound);
                    await _context.SaveChangesAsync();
                    
                    await FollowupAsync("Text to speech playing.").ConfigureAwait(false);
                    using (var ffmpeg = CreateProcess($"C:\\Workspace_Git\\BigOne\\BigOne\\Sounds\\{name.Replace(" ", "_")}.mp3"))
                    using (var stream = audioClient.CreatePCMStream(AudioApplication.Mixed))
                    {
                        try { await ffmpeg.StandardOutput.BaseStream.CopyToAsync(stream); }
                        finally { await stream.FlushAsync(); }
                    }
                }
            }
            else
            {
                Console.WriteLine($"Failed to synthesize speech. Status code: {response.StatusCode}, Message: {response.ReasonPhrase}");
                await FollowupAsync("Text to speech failed");
            }
        }
    }

    public Process CreateProcess(string path)
    {
        Process process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -ar 48000 -f s16le -acodec pcm_s16le pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };
        process.Start();
        process.ErrorDataReceived += (sender, e) => Console.WriteLine("FFmpeg Error: " + e.Data);
        process.BeginErrorReadLine();
        return process;
    }
}
