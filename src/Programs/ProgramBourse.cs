namespace NonogramAutomation
{
    public class ProgramBourse : Program
    {
        public ProgramBourse(ADBInstance adbInstance, CancellationToken token)
             : base(adbInstance, token)
        {
        }

        private enum BourseItem
        {
            TreasureMap,
            Coffee,
            Katana,
            Potion
        }

        public override async Task StartAsync()
        {
            while (true)
            {
                try
                {
                    await using UndoActions undoActions = new();

                    await _adbInstance.StartEmulator(_token);
                    undoActions.Add(async () => await _adbInstance.StopEmulator());

                    await _adbInstance.ConnectToInstanceAsync(_token);
                    undoActions.Add(async () => await _adbInstance.DisconnectFromInstanceAsync());

                    await _adbInstance.StartApplicationAsync(_token);
                    undoActions.Add(async () => await _adbInstance.StopApplicationAsync());

                    await ClickOnGuildAsync(TimeSpan.FromMinutes(2), _token);
                    await ClickOnBourseAsync(TimeSpan.FromSeconds(10), _token);
                    await ScrollAndClickOnItemAsync(BourseItem.Katana, TimeSpan.FromSeconds(30), _token);
                    await WaitForRewardAsync(TimeSpan.FromSeconds(60), _token);

                    await ReturnToMainMenuAsync(TimeSpan.FromSeconds(60), _token);
                    await ClickOnSettingsAsync(TimeSpan.FromSeconds(10), _token);
                    await ClickOnOtherAsync(TimeSpan.FromSeconds(10), _token);
                    await ClickOnSaveZipAsync(TimeSpan.FromSeconds(10), _token);
                    System.IO.File.Delete("C:\\Users\\dotte\\Downloads\\NonogramsKatana.zip");
                    System.IO.File.Move("C:\\Users\\dotte\\Documents\\MuMuSharedFolder\\NonogramsKatana.zip", "C:\\Users\\dotte\\Downloads\\NonogramsKatana.zip");
                    await ClickOnSaveAsync(TimeSpan.FromSeconds(10), _token);
                    await ReturnToMainMenuAsync(TimeSpan.FromSeconds(60), _token);
                }
                catch (Exception exception)
                {
                    Logger.Log(Logger.LogLevel.Warning, _adbInstance.LogHeader, $"<@{SettingsManager.GlobalSettings.DiscordUserId}> An exception has been raised:{exception}");
                }
            }
        }

        private async Task ClickOnGuildAsync(TimeSpan timeout, CancellationToken token)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, _adbInstance.LogHeader);

            await Task.Delay(TimeSpan.FromSeconds(5), token);
            AdvancedSharpAdbClient.DeviceCommands.Models.Element? guildButton = await Utils.FindElementAsync(_adbInstance, "//node[@resource-id='com.ucdevs.jcross:id/btnGuild']", TimeSpan.FromSeconds(2), token);
            if (guildButton is not null)
            {
                await guildButton.ClickAsync(token);
                return;
            }

            bool isErrorMessage = await Utils.DetectElementAsync(_adbInstance, "//node[@text='Warning: Guild last saved progress is not accessible, loaded from previous slot.']", timeout, token);
            if (isErrorMessage)
            {
                await Utils.ClickBackButtonAsync(_adbInstance, token);
                await ClickOnSettingsAsync(TimeSpan.FromSeconds(10), token);
                await ClickOnOtherAsync(TimeSpan.FromSeconds(10), token);
                await ClickOnLoadZipAsync(TimeSpan.FromSeconds(10), token);
                await ClickOnLoadAsync(TimeSpan.FromSeconds(10), token);
                await ReturnToMainMenuAsync(TimeSpan.FromSeconds(60), _token);
                await ClickOnGuildAsync(TimeSpan.FromSeconds(30), token);
                return;
            }
        }

        private async Task ClickOnSettingsAsync(TimeSpan timeout, CancellationToken token)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, _adbInstance.LogHeader);

            await Utils.ClickElementAsync(_adbInstance, "//node[@resource-id='com.ucdevs.jcross:id/action_settings']", timeout, token);
        }

        private async Task ClickOnOtherAsync(TimeSpan timeout, CancellationToken token)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, _adbInstance.LogHeader);

            await Utils.ClickElementAsync(_adbInstance, "//node[@text='Autre']", timeout, token);
        }

        private async Task ClickOnLoadZipAsync(TimeSpan timeout, CancellationToken token)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, _adbInstance.LogHeader);

            await Utils.ClickElementAsync(_adbInstance, "//node[@text='Charger la progression du fichier (zip)']", timeout, token);
        }

        private async Task ClickOnLoadAsync(TimeSpan timeout, CancellationToken token)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, _adbInstance.LogHeader);

            await Utils.ClickElementAsync(_adbInstance, "//node[@text='NonogramsKatana.zip']", timeout, token);
        }

        private async Task ClickOnSaveZipAsync(TimeSpan timeout, CancellationToken token)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, _adbInstance.LogHeader);

            await Utils.ClickElementAsync(_adbInstance, "//node[@text='Sauvegarder la progression dans le fichier (zip)']", timeout, token);
        }

        private async Task ClickOnSaveAsync(TimeSpan timeout, CancellationToken token)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, _adbInstance.LogHeader);

            await Utils.ClickElementAsync(_adbInstance, "//node[@text='ENREGISTRER']", timeout, token);
        }

        private async Task ClickOnBourseAsync(TimeSpan timeout, CancellationToken token)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, _adbInstance.LogHeader);

            await Utils.ClickElementAsync(_adbInstance, "//node[@resource-id='com.ucdevs.jcross:id/catBourse']", timeout, token);
        }

        private async Task ScrollAndClickOnItemAsync(BourseItem item, TimeSpan timeout, CancellationToken parentToken)
        {
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(parentToken, timeoutCts.Token);

            using LogContext logContext = new(Logger.LogLevel.Debug, _adbInstance.LogHeader);

            string itemAsString = item switch
            {
                BourseItem.TreasureMap => "Fragment",
                BourseItem.Coffee => "Grains",
                BourseItem.Katana => "Katana",
                BourseItem.Potion => "Potion",
                _ => throw new NotImplementedException()
            };
            string query = $"//node[@resource-id='com.ucdevs.jcross:id/clickItem'][descendant::node[@resource-id='com.ucdevs.jcross:id/tvSellName' and contains(@text, '{itemAsString}')] and descendant::node[@resource-id='com.ucdevs.jcross:id/tvBuyName' and @text='Regarder la pub']]";

            while (true)
            {
                linkedCts.Token.ThrowIfCancellationRequested();

                AdvancedSharpAdbClient.DeviceCommands.Models.Element? element = await Utils.FindElementAsync(_adbInstance, query, TimeSpan.FromSeconds(2), linkedCts.Token);
                if (element is not null)
                {
                    Logger.Log(Logger.LogLevel.Info, _adbInstance.LogHeader, $"Clicking on {item}");
                    await element.ClickAsync(linkedCts.Token);
                    await Task.Delay(TimeSpan.FromMilliseconds(100), linkedCts.Token);
                    return;
                }

                Logger.Log(Logger.LogLevel.Info, _adbInstance.LogHeader, $"Scrolling to find {item}");
                await Utils.SwipeToBottomAsync(_adbInstance, linkedCts.Token);
            }
        }

        private async Task WaitForRewardAsync(TimeSpan timeout, CancellationToken parentToken)
        {
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(parentToken, timeoutCts.Token);

            using LogContext logContext = new(Logger.LogLevel.Debug, _adbInstance.LogHeader);

            await Task.Delay(TimeSpan.FromSeconds(5), linkedCts.Token);

            if (await Utils.DetectElementAsync(_adbInstance, "//node[@resource-id='contain-paidtasks-survey']", TimeSpan.FromSeconds(2), linkedCts.Token))
            {
                throw new Exception("Survey detected, cannot continue");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), linkedCts.Token);

            await Utils.DumpAllAsync(_adbInstance, "Rewards", false, linkedCts.Token);
        }

        private async Task ReturnToMainMenuAsync(TimeSpan timeout, CancellationToken parentToken)
        {
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(parentToken, timeoutCts.Token);

            using LogContext logContext = new(Logger.LogLevel.Debug, _adbInstance.LogHeader);

            while (true)
            {
                linkedCts.Token.ThrowIfCancellationRequested();

                await Utils.ClickBackButtonAsync(_adbInstance, linkedCts.Token);
                await Task.Delay(TimeSpan.FromSeconds(1), linkedCts.Token);
                bool isOnMainMenu = await Utils.DetectElementAsync(_adbInstance, "//node[@resource-id='com.ucdevs.jcross:id/btnGuild']", TimeSpan.FromSeconds(2), linkedCts.Token);
                if (isOnMainMenu)
                {
                    Logger.Log(Logger.LogLevel.Info, _adbInstance.LogHeader, $"Back to main menu");
                    return;
                }
            }
        }

        private async Task ClickOnUserAsync(TimeSpan timeout, CancellationToken token)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, _adbInstance.LogHeader);

            await Utils.ClickElementAsync(_adbInstance, "//node[@resource-id='com.ucdevs.jcross:id/userBtn']", timeout, token);
        }

        private async Task ClickOnSyncNow(TimeSpan timeout, CancellationToken token)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, _adbInstance.LogHeader);

            await Utils.ClickElementAsync(_adbInstance, "//node[@resource-id='com.ucdevs.jcross:id/btnSyncNow']", timeout, token);
        }
    }
}