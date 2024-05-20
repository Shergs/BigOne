

namespace BigOneDashboard.Models
{
    public class DashboardViewModel
    {
        public DashboardViewModel()
        { 
            SaveNewSoundViewModel = new SaveNewSoundViewModel();
            EditSoundViewModel = new EditSoundViewModel();
            DeleteSoundViewModel = new DeleteSoundViewModel();
            AvailableGuilds = new List<Guild>();
            Guild = new Guild();
            Sounds = new List<Sound>();
            serverId = "";
            DiscordName = "";
            embedUrl = "";
        }
        public SaveNewSoundViewModel SaveNewSoundViewModel { get; set; }
        public EditSoundViewModel EditSoundViewModel { get; set; }
        public DeleteSoundViewModel DeleteSoundViewModel { get; set; }
        public List<Sound> Sounds { get; set; }
        public string DiscordName { get; set; }
        public List<Guild> AvailableGuilds { get; set; }
        public Guild Guild { get; set; }
        public string serverId { get; set; }
        public string botUrl { get; set; }
        public Song Song { get; set; }
        public List<Song> Songs { get; set; }
        public List<SongHistoryItem> SongHistory { get; set; }
        public int Position { get; set; }
        public string embedUrl { get; set; }
        public string dashboardBaseUrl { get; set; }
        public List<Voice> Voices { get; set; }
        public Dictionary<string, string> VoiceChannels { get; set; }
    }
}
