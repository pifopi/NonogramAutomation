namespace NonogramAutomation.Models
{
    public class GlobalSettings
    {
        public ulong DiscordChannelId { get; set; }
        public ulong DiscordUserId { get; set; }
        public string MuMuPath { get; set; } = "";
        public List<MuMuSettings> MuMuInstances { get; set; } = [];
        public List<RealPhoneSettings> RealPhoneInstances { get; set; } = [];
    }
}
