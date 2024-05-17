using Google.Apis.Auth.OAuth2.Responses;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace BigOneDashboard.Areas.DiscordAuth
{
    public class TokenService
    {
        private readonly ApplicationDbContext _context; // Assuming you store tokens in a database
        private readonly IConfiguration _configuration;

        public TokenService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<string> GetAccessTokenAsync(string userId)
        {
            var userToken = await _context.UserTokens.FirstOrDefaultAsync(ut => ut.UserId == userId);

            if (userToken == null)
            {
                throw new Exception("User token not found.");
            }

            // Check if the current access token is still valid
            if (userToken.Expiry > DateTime.UtcNow)
            {
                return userToken.AccessToken;
            }
            else
            {
                // if this returns empty string, make the user log in again
                return await RefreshAccessTokenAsync(userToken);
            }
        }

        private async Task<string> RefreshAccessTokenAsync(UserToken userToken)
        {
            var client = new HttpClient();
            var postData = new Dictionary<string, string>
            {
                {"client_id", _configuration["Discord:ClientId"] ?? ""},
                {"client_secret", _configuration["Discord:ClientSecret"] ?? ""},
                {"grant_type", "refresh_token"},
                {"refresh_token", userToken.RefreshToken}
            };

            var requestContent = new FormUrlEncodedContent(postData);
            var response = await client.PostAsync("https://discord.com/api/oauth2/token", requestContent);

            if (!response.IsSuccessStatusCode)
            {
                return "";
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var newTokens = JsonConvert.DeserializeObject<TokenResponse>(responseContent);

            // Update the database with the new access token and expiry time
            userToken.AccessToken = newTokens?.AccessToken ?? "";
            userToken.Expiry = DateTime.UtcNow.AddSeconds((double)newTokens.ExpiresInSeconds);
            _context.Update(userToken);
            await _context.SaveChangesAsync();

            return newTokens.AccessToken;
        }
    }
}
