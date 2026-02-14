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

        public override async Task StartEmulator(CancellationToken token)
        {
            using LogContext logContext = new(Logger.LogLevel.Info, LogHeader);
            Utils.ExecuteCmd(System.IO.Path.Combine(SettingsManager.GlobalSettings.MuMuPath, "nx_device", "12.0", "shell", "MuMuNxDevice.exe"), "");

            while (true)
            {
                System.Diagnostics.Process process = new()
                {
                    StartInfo = new()
                    {
                        FileName = System.IO.Path.Combine(SettingsManager.GlobalSettings.MuMuPath, "nx_main", "MuMuManager.exe"),
                        Arguments = $"info --vmindex {MuMuId}",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        StandardOutputEncoding = System.Text.Encoding.UTF8
                    }
                };
                process.Start();
                string jsonOutput = await process.StandardOutput.ReadToEndAsync(token);
                process.WaitForExit();

                if (!string.IsNullOrWhiteSpace(jsonOutput))
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(jsonOutput);
                    System.Text.Json.JsonElement playerState = new();
                    if (doc.RootElement.TryGetProperty("player_state", out playerState) &&
                        playerState.GetString() == "start_finished")
                    {
                        break;
                    }
                }

            }
        }

        public override async Task StopEmulator()
        {
            using LogContext logContext = new(Logger.LogLevel.Info, LogHeader);
            Utils.ExecuteCmd("taskkill", "/IM MuMuNxDevice.exe /F");
            await Task.Delay(TimeSpan.FromSeconds(2));
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
