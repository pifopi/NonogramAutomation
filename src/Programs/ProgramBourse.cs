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
            Carte,
            Café,
            Katana,
            Potion
        }

        public override async Task StartAsync()
        {
            while (true)
            {
                try
                {
                    await _adbInstance.ConnectToInstanceAsync(_token);

                    await Task.Delay(TimeSpan.FromMinutes(20), _token);
                    await GoToBourseAsync(TimeSpan.FromSeconds(10), _token);
                    await ScrollAndClickOnItemAsync(BourseItem.Katana, TimeSpan.FromSeconds(30), _token);
                    await WaitForReward(TimeSpan.FromSeconds(60), _token);
                    await ReturnToMainMenuAsync(TimeSpan.FromSeconds(60), _token);
                }
                catch (Exception exception)
                {
                    Logger.Log(Logger.LogLevel.Warning, _adbInstance.LogHeader, $"<@{SettingsManager.GlobalSettings.DiscordUserId}> An exception has been raised:{exception}");
                }
            }
        }

        private async Task GoToBourseAsync(TimeSpan timeout, CancellationToken token)
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
                BourseItem.Carte => "Fragment",
                BourseItem.Café => "Grains",
                BourseItem.Katana => "Katana",
                BourseItem.Potion => "Potion",
                _ => throw new NotImplementedException()
            };
            string query = $"//node[@resource-id='com.ucdevs.jcross:id/clickItem'][descendant::node[@resource-id='com.ucdevs.jcross:id/tvSellName' and contains(@text, '{itemAsString}')] and descendant::node[@resource-id='com.ucdevs.jcross:id/tvBuyName' and @text='Regarder la pub']]";

            while (true)
            {
                linkedCts.Token.ThrowIfCancellationRequested();

                using var searchTimeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                using var searchLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(linkedCts.Token, searchTimeoutCts.Token);

                AdvancedSharpAdbClient.DeviceCommands.Models.Element? element = await Utils.FindElementAsync(_adbInstance, query, searchLinkedCts.Token);
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

        private async Task WaitForReward(TimeSpan timeout, CancellationToken parentToken)
        {
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(parentToken, timeoutCts.Token);

            using LogContext logContext = new(Logger.LogLevel.Debug, _adbInstance.LogHeader);

            await Task.Delay(TimeSpan.FromSeconds(45), linkedCts.Token);

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
                bool isOnMainMenu = await Utils.DetectElementAsync(_adbInstance, "//node[@resource-id='com.ucdevs.jcross:id/catBourse']", TimeSpan.FromSeconds(2), linkedCts.Token);
                if (isOnMainMenu)
                {
                    Logger.Log(Logger.LogLevel.Info, _adbInstance.LogHeader, $"Back to main menu");
                    return;
                }
            }
        }
    }
}