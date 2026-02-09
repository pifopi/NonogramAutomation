namespace NonogramAutomation
{
    public abstract class ADBInstanceViaIP : ADBInstance
    {
        private string _ip = "127.0.0.1";
        private int _port = 5555;

        public string IP
        {
            get => _ip;
            set { _ip = value; OnPropertyChanged(); }
        }

        public int Port
        {
            get => _port;
            set { _port = value; OnPropertyChanged(); }
        }

        protected override async Task ConnectToInstanceAsync(CancellationToken token)
        {
            Port = GetPort();

            string resultConnect = await AdbClient.ConnectAsync(IP, Port, token);
            if (resultConnect != $"connected to {IP}:{Port}" &&
                resultConnect != $"already connected to {IP}:{Port}")
            {
                throw new Exception(resultConnect);
            }
            DeviceData = await Utils.GetDeviceDataFromAsync(AdbClient, $"{IP}:{Port}", TimeSpan.FromMinutes(1), token);
        }

        protected override async Task DisconnectFromInstanceAsync()
        {
            try
            {
                await AdbClient.DisconnectAsync(IP, Port);
            }
            catch (Exception exception)
            {
                Logger.Log(Logger.LogLevel.Warning, LogHeader, $"<@{SettingsManager.GlobalSettings.DiscordUserId}> An exception has been raised:{exception}");
            }
            finally
            {
                DeviceData = new();
            }
        }

        protected virtual int GetPort()
        {
            return Port;
        }
    }
}
