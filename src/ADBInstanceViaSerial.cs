namespace NonogramAutomation
{
    public abstract class ADBInstanceViaSerial : ADBInstance
    {
        private string _serialName = "emulator-5555";

        public string SerialName
        {
            get => _serialName;
            set { _serialName = value; OnPropertyChanged(); }
        }

        protected override async Task ConnectToInstanceAsync(CancellationToken token)
        {
            DeviceData = await Utils.GetDeviceDataFromAsync(AdbClient, SerialName, TimeSpan.FromMinutes(1), token);
        }

        protected override async Task DisconnectFromInstanceAsync()
        {
            DeviceData = new();
        }
    }
}
