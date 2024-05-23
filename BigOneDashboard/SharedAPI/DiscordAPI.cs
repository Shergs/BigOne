using BigOneDashboard.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using BigOneDashboard.Data;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using AspNet.Security.OAuth.Discord;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using BigOneData.Migrations;
using System.Text.Json;
using Humanizer.Configuration;
using System.Threading.Channels;

namespace BigOneDashboard.SharedAPI
{
    public static class DiscordAPI
    {
        public static async Task<string> GetUserGuilds(string token)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                return await client.GetStringAsync("https://discord.com/api/v9/users/@me/guilds");
            }
        }

        public static async Task<string> GetBotGuilds(string token)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bot", token);
                return await client.GetStringAsync("https://discord.com/api/v9/users/@me/guilds");
            }
        }

        public static async Task<string> GetUserInfo(string token)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var result = await client.GetStringAsync("https://discord.com/api/v9/users/@me");
                using (var doc = JsonDocument.Parse(result))
                {
                    var username = doc.RootElement.GetProperty("username").GetString() ?? "";
                    var userId = doc.RootElement.GetProperty("id").GetString() ?? "";
                    UserData userData = new UserData
                    {
                        UserId = userId,
                        Username = username
                    };
                    return JsonConvert.SerializeObject(userData);
                }
            }
        }

        public static async Task<List<GuildChannel>?> GetGuildChannels(string token, string guildId)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bot", token);
                var result = await client.GetStringAsync($"https://discord.com/api/v9/guilds/{guildId}/channels");
                List<GuildChannel>? channels = JsonConvert.DeserializeObject<List<GuildChannel>>(result);
                return channels ?? new List<GuildChannel>();
            }
        }
    }
}
