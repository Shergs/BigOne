namespace BigOneDashboard.Models
{
    public class DashboardViewModel
    {
        public DashboardViewModel()
        { 
            SaveNewSoundViewModel = new SaveNewSoundViewModel();
            EditSoundViewModel = new EditSoundViewModel();
            DeleteSoundViewModel = new DeleteSoundViewModel();
            Sounds = new List<Sound>();
            Server = "";
            DiscordName = "";
        }
        public SaveNewSoundViewModel SaveNewSoundViewModel { get; set; }
        public EditSoundViewModel EditSoundViewModel { get; set; }
        public DeleteSoundViewModel DeleteSoundViewModel { get; set; }
        public List<Sound> Sounds { get; set; }

        public string Server { get; set; }
        public string DiscordName { get; set; }
    }
}
