namespace NonogramAutomation
{
    public class ADBInstanceMuMu : ADBInstanceViaIP
    {
        private int _mumuId = 1;

        public int MuMuId
        {
            get => _mumuId;
            set { _mumuId = value; OnPropertyChanged(); }
        }

        public override string LogHeader
        {
            get => $"{Name} | {IP}:{Port} | {MuMuId}";
        }

        protected override int GetPort()
        {
            string configFile = System.IO.Path.Combine(SettingsManager.GlobalSettings.MuMuPath, "vms", $"MuMuPlayerGlobal-12.0-{MuMuId}", "configs", "vm_config.json");
            string jsonContent = System.IO.File.ReadAllText(configFile);
            Newtonsoft.Json.Linq.JObject jsonFile = Newtonsoft.Json.Linq.JObject.Parse(jsonContent);
            string? portAsString = jsonFile.SelectToken("vm")?.SelectToken("nat")?.SelectToken("port_forward")?.SelectToken("adb")?.SelectToken("host_port")?.ToString();
            if (portAsString == null)
            {
                throw new Exception("The adb config cannot be found");
            }
            return int.Parse(portAsString);
        }
    }
}
