namespace BigOneDashboard.Models
{
    public class SaveNewSoundViewModel
    {
        public SaveNewSoundViewModel()
        { }
        public SaveNewSoundViewModel(string server)
        {
            serverId = server;
        }
        public string Name { get; set; }
        public IFormFile File { get; set; }
        public string Emote { get; set; }
        public string serverId { get; set; }
    }
}
