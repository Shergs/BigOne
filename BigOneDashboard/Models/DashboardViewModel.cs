

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
        }
        public SaveNewSoundViewModel SaveNewSoundViewModel { get; set; }
        public EditSoundViewModel EditSoundViewModel { get; set; }
        public DeleteSoundViewModel DeleteSoundViewModel { get; set; }
        public List<Sound> Sounds { get; set; }
        public string DiscordName { get; set; }
        public List<Guild> AvailableGuilds { get; set; }
        public Guild Guild { get; set; }
        public string serverId { get; set; }
    }
}
