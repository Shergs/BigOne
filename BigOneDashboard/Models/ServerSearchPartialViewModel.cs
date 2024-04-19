namespace BigOneDashboard.Models
{
    public class ServerSearchPartialViewModel
    {
        public ServerSearchPartialViewModel()
        { 
            AvailableGuilds = new List<Guild>();
        }
        public List<Guild> AvailableGuilds { get; set; }
    }
}
