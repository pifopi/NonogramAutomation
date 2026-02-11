namespace NonogramAutomation
{
    public class ProgramDump : Program
    {
        public ProgramDump(ADBInstance adbInstance, CancellationToken token)
             : base(adbInstance, token)
        {
        }

        public override async Task StartAsync()
        {
            try
            {
                await _adbInstance.ConnectToInstanceAsync(_token);

                await Utils.DumpAllAsync(_adbInstance, "Dump", true, _token);
            }
            catch (Exception exception)
            {
                Logger.Log(Logger.LogLevel.Warning, _adbInstance.LogHeader, $"<@{SettingsManager.GlobalSettings.DiscordUserId}> An exception has been raised:{exception}");
            }
        }
    }
}