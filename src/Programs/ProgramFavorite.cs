namespace NonogramAutomation
{
    public class ProgramFavorite : Program
    {
        public ProgramFavorite(ADBInstance adbInstance, CancellationToken token)
             : base(adbInstance, token)
        {
        }

        public override async Task StartAsync()
        {
            try
            {
                await _adbInstance.ConnectToInstanceAsync(_token);
                while (true)
                {
                    await GoToPuzzleDetailsMenuAsync(TimeSpan.FromSeconds(10), _token);
                    await RemoveFavoritePuzzleAsync(TimeSpan.FromSeconds(10), _token);
                }
            }
            catch (Exception exception)
            {
                Logger.Log(Logger.LogLevel.Warning, _adbInstance.LogHeader, $"<@{SettingsManager.GlobalSettings.DiscordUserId}> An exception has been raised:{exception}");
            }
        }

        private async Task GoToPuzzleDetailsMenuAsync(TimeSpan timeout, CancellationToken token)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, _adbInstance.LogHeader);

            await Utils.ClickElementAsync(_adbInstance, "//node[@resource-id='com.ucdevs.jcross:id/btnCtxMenu']", timeout, token);
        }

        private async Task RemoveFavoritePuzzleAsync(TimeSpan timeout, CancellationToken token)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, _adbInstance.LogHeader);

            await Utils.ClickElementAsync(_adbInstance, "//node[@resource-id='com.ucdevs.jcross:id/removeFavorites']", timeout, token);
        }
    }
}