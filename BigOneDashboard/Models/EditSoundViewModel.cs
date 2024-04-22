namespace BigOneDashboard.Models
{
    public class EditSoundViewModel
    {
        public EditSoundViewModel()
        { 
        
        }
        public EditSoundViewModel(string server)
        {
            serverId = server;
        }
        public int Id { get; set; }
        public string Name { get; set; }
        public string Emote { get; set; }
        public string serverId { get; set; }
    }
}
