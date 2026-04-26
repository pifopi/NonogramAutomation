namespace NonogramAutomation
{
    public abstract class ProgramBourse : Program
    {
        public ProgramBourse(ADBInstance adbInstance, CancellationToken token)
             : base(adbInstance, token)
        {
        }

        protected enum BourseItem
        {
            TreasureMap,
            CoffeeBean,
            Katana,
            Potion
        }

        protected enum ActionWhenFull
        {
            Stop,
            Continue
        }

        protected async Task StartAsync(BourseItem item, ActionWhenFull actionWhenFull)
        {
            int collectTimes = 0;

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

                    await ClickOnGuildAsync(TimeSpan.FromSeconds(10), _token);
                    await ClickOnBourseAsync(TimeSpan.FromSeconds(10), _token);
                    await ScrollUntilItemAsync(item, TimeSpan.FromSeconds(10), _token);
                    await ClickOnItemAsync(item, TimeSpan.FromSeconds(10), _token);
                    try
                    {
                        await WaitForAdAsync(item, TimeSpan.FromSeconds(10), _token);
                    }
                    catch (NoRoomForStorageException exception)
                    {
                        while (true)
                        {
                            Logger.Log(Logger.LogLevel.Warning, _adbInstance.LogHeader, $"<@{SettingsManager.GlobalSettings.DiscordUserId}> An exception has been raised:{exception}");
                            if (actionWhenFull == ActionWhenFull.Continue)
                            {
                                throw;
                            }
                            await Task.Delay(TimeSpan.FromMinutes(1));
                        }
                    }
                    catch (NoAdLoadedException exception)
                    {
                        Logger.Log(Logger.LogLevel.Warning, _adbInstance.LogHeader, $"<@{SettingsManager.GlobalSettings.DiscordUserId}> An exception has been raised:{exception}");
                        await Utils.ClickHomeButtonAsync(_adbInstance, _token);
                        await ClickOnSettingsAppAsync(TimeSpan.FromSeconds(10), _token);
                        await ClickOnConfidentialityAsync(TimeSpan.FromSeconds(10), _token);
                        await ClickOnAdsAsync(TimeSpan.FromSeconds(10), _token);
                        await ClickOnResetAdsIDAsync(TimeSpan.FromSeconds(10), _token);
                        await ClickOnConfirmAsync(TimeSpan.FromSeconds(10), _token);
                        throw;
                    }

                    await ReturnToMainMenuAsync(TimeSpan.FromSeconds(90), _token);
                    collectTimes++;
                    int countPerCollect = item switch
                    {
                        BourseItem.TreasureMap => 2,
                        BourseItem.CoffeeBean => 2,
                        BourseItem.Katana => 5,
                        BourseItem.Potion => 1,
                        _ => throw new NotImplementedException()
                    };
                    Logger.Log(Logger.LogLevel.Info, _adbInstance.LogHeader, $"Successfully collected {item} (collected {collectTimes} times(s) for {collectTimes * countPerCollect} item(s))");
                }
                catch (NoRoomForStorageException)
                {
                    break;
                }
                catch (NoAdLoadedException)
                {
                    continue;
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

            await Utils.ClickElementAsync(_adbInstance, "//node[@resource-id='com.ucdevs.jcross:id/btnGuild']", timeout, token);
        }

        private async Task ClickOnBourseAsync(TimeSpan timeout, CancellationToken token)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, _adbInstance.LogHeader);

            await Utils.ClickElementAsync(_adbInstance, "//node[@resource-id='com.ucdevs.jcross:id/catBourse']", timeout, token);
        }

        private async Task ScrollUntilItemAsync(BourseItem item, TimeSpan timeout, CancellationToken parentToken)
        {
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(parentToken, timeoutCts.Token);

            using LogContext logContext = new(Logger.LogLevel.Debug, _adbInstance.LogHeader);

            List<string> queries = new()
            {
                GetItemQuery(item)
            };
            for (int i = 20; i > 0; i--)
            {
                queries.Add(GetItemQuery(item, $"0:{i:D2}"));
            }

            while (true)
            {
                linkedCts.Token.ThrowIfCancellationRequested();

                FoundElement? foundElement = await Utils.FindElementAsync(_adbInstance, queries, TimeSpan.FromSeconds(2), linkedCts.Token);
                if (foundElement is null)
                {
                    Logger.Log(Logger.LogLevel.Info, _adbInstance.LogHeader, $"Scrolling to find {item}");
                    await Utils.SwipeToBottomAsync(_adbInstance, linkedCts.Token);
                    continue;
                }

                if (foundElement.Index == 0)
                {
                    return;
                }
                else if (foundElement.Index >= 1 && foundElement.Index <= 20)
                {
                    throw new Exception("Bourse timer is not ready");
                }
                else
                {
                    throw new Exception("Unexpected element index");
                }
            }
        }

        private async Task ClickOnItemAsync(BourseItem item, TimeSpan timeout, CancellationToken token)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, _adbInstance.LogHeader);

            await Utils.ClickElementAsync(_adbInstance, GetItemQuery(item), timeout, token);
        }

        private async Task WaitForAdAsync(BourseItem item, TimeSpan timeout, CancellationToken token)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, _adbInstance.LogHeader);

            List<string> queries = new()
            {
                "//node[@resource-id='contain-paidtasks-survey']",
                "//node[@text=\"Pas d'espace disponible dans l'entrepôt\"]",
                GetItemQuery(item)
            };

            FoundElement? foundElement = await Utils.FindElementAsync(_adbInstance, queries, timeout, token);
            if (foundElement is null)
            {
                Logger.Log(Logger.LogLevel.Info, _adbInstance.LogHeader, "Ad loaded properly");
                return;
            }
            switch (foundElement.Index)
            {
                case 0:
                    throw new Exception("Survey detected");
                case 1:
                    throw new NoRoomForStorageException();
                case 2:
                    throw new NoAdLoadedException();
                default:
                    throw new Exception("Unexpected element index");
            }
        }

        private async Task ReturnToMainMenuAsync(TimeSpan timeout, CancellationToken parentToken)
        {
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(parentToken, timeoutCts.Token);

            using LogContext logContext = new(Logger.LogLevel.Debug, _adbInstance.LogHeader);

            while (true)
            {
                linkedCts.Token.ThrowIfCancellationRequested();

                await Task.Delay(TimeSpan.FromSeconds(1));
                if (await Utils.FindElementAsync(_adbInstance, "//node[@resource-id='com.ucdevs.jcross:id/btnGuild']", TimeSpan.FromSeconds(2), linkedCts.Token) is not null)
                {
                    Logger.Log(Logger.LogLevel.Info, _adbInstance.LogHeader, $"Back to main menu");
                    return;
                }
                await Utils.ClickBackButtonAsync(_adbInstance, linkedCts.Token);
            }
        }

        private async Task ClickOnSettingsAppAsync(TimeSpan timeout, CancellationToken token)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, _adbInstance.LogHeader);

            await Utils.ClickElementAsync(_adbInstance, "//node[@text='Paramètres']", timeout, token);
        }

        private async Task ClickOnConfidentialityAsync(TimeSpan timeout, CancellationToken token)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, _adbInstance.LogHeader);

            await Utils.ClickElementAsync(_adbInstance, "//node[@text='Confidentialité']", timeout, token);
        }

        private async Task ClickOnAdsAsync(TimeSpan timeout, CancellationToken token)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, _adbInstance.LogHeader);

            await Utils.ClickElementAsync(_adbInstance, "//node[@text='Annonces']", timeout, token);
        }

        private async Task ClickOnResetAdsIDAsync(TimeSpan timeout, CancellationToken token)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, _adbInstance.LogHeader);

            await Utils.ClickElementAsync(_adbInstance, "//node[@text=\"Réinitialiser l'identifiant publicitaire\"]", timeout, token);
        }

        private async Task ClickOnConfirmAsync(TimeSpan timeout, CancellationToken token)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, _adbInstance.LogHeader);

            await Utils.ClickElementAsync(_adbInstance, "//node[@text='CONFIRMER']", timeout, token);
        }

        private string GetItemQuery(BourseItem item, string suffix = "Regarder la pub")
        {
            string itemAsString = item switch
            {
                BourseItem.TreasureMap => "Fragment",
                BourseItem.CoffeeBean => "Grains",
                BourseItem.Katana => "Katana",
                BourseItem.Potion => "Potion",
                _ => throw new NotImplementedException()
            };

            return $"//node[@resource-id='com.ucdevs.jcross:id/clickItem'][descendant::node[@resource-id='com.ucdevs.jcross:id/tvSellName' and contains(@text, '{itemAsString}')] and descendant::node[@resource-id='com.ucdevs.jcross:id/tvBuyName' and @text='{suffix}']]";
        }

        private class NoRoomForStorageException : Exception
        {
            public NoRoomForStorageException()
            {
            }
        }

        private class NoAdLoadedException : Exception
        {
            public NoAdLoadedException()
            {
            }
        }
    }

    public class ProgramBourseTreasureMap : ProgramBourse
    {
        public ProgramBourseTreasureMap(ADBInstance adbInstance, CancellationToken token)
             : base(adbInstance, token)
        {
        }

        public override async Task StartAsync()
        {
            await StartAsync(BourseItem.TreasureMap, ActionWhenFull.Stop);
        }
    }

    public class ProgramBourseCoffeeBean : ProgramBourse
    {
        public ProgramBourseCoffeeBean(ADBInstance adbInstance, CancellationToken token)
             : base(adbInstance, token)
        {
        }

        public override async Task StartAsync()
        {
            await StartAsync(BourseItem.CoffeeBean, ActionWhenFull.Stop);
        }
    }

    public class ProgramBourseKatana : ProgramBourse
    {
        public ProgramBourseKatana(ADBInstance adbInstance, CancellationToken token)
             : base(adbInstance, token)
        {
        }

        public override async Task StartAsync()
        {
            await StartAsync(BourseItem.Katana, ActionWhenFull.Stop);
        }
    }

    public class ProgramBoursePotion : ProgramBourse
    {
        public ProgramBoursePotion(ADBInstance adbInstance, CancellationToken token)
             : base(adbInstance, token)
        {
        }

        public override async Task StartAsync()
        {
            await StartAsync(BourseItem.Potion, ActionWhenFull.Stop);
        }
    }

    public class ProgramBourseAll : ProgramBourse
    {
        public ProgramBourseAll(ADBInstance adbInstance, CancellationToken token)
             : base(adbInstance, token)
        {
        }

        public override async Task StartAsync()
        {
            await StartAsync(BourseItem.Katana, ActionWhenFull.Continue);
            await StartAsync(BourseItem.CoffeeBean, ActionWhenFull.Continue);
            await StartAsync(BourseItem.Potion, ActionWhenFull.Continue);
            await StartAsync(BourseItem.TreasureMap, ActionWhenFull.Stop);
        }
    }
}