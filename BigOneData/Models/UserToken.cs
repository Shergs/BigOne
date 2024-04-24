public class UserToken
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public DateTime Expiry { get; set; }
}
