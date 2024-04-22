namespace BigOneDashboard.Models
{
    public class DeleteSoundViewModel
    {
        public DeleteSoundViewModel()
        {

        }
        public DeleteSoundViewModel(string server)
        {
            serverId = server;
        }
        public int Id { get; set; }
        public string serverId { get; set; }
    }
}
