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
            int itemCount = 0;

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
                    try
                    {
                        await ClickOnItemAsync(item, TimeSpan.FromSeconds(30), _token);
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

                    await ReturnToMainMenuAsync(TimeSpan.FromSeconds(60), _token);
                    itemCount++;
                    Logger.Log(Logger.LogLevel.Info, _adbInstance.LogHeader, $"Successfully bought {item} {itemCount} time(s)");
                }
                catch (NoRoomForStorageException)
                {
                    break;
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

            string itemAsString = item switch
            {
                BourseItem.TreasureMap => "Fragment",
                BourseItem.CoffeeBean => "Grains",
                BourseItem.Katana => "Katana",
                BourseItem.Potion => "Potion",
                _ => throw new NotImplementedException()
            };

            List<string> queries = new()
            {
                $"//node[@resource-id='com.ucdevs.jcross:id/clickItem'][descendant::node[@resource-id='com.ucdevs.jcross:id/tvSellName' and contains(@text, '{itemAsString}')] and descendant::node[@resource-id='com.ucdevs.jcross:id/tvBuyName' and @text='Regarder la pub']]"
            };
            for (int i = 20; i > 0; i--)
            {
                queries.Add($"//node[@resource-id='com.ucdevs.jcross:id/clickItem'][descendant::node[@resource-id='com.ucdevs.jcross:id/tvSellName' and contains(@text, '{itemAsString}')] and descendant::node[@resource-id='com.ucdevs.jcross:id/tvBuyName' and @text='0:{i:D2}']]");
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

        private async Task ClickOnItemAsync(BourseItem item, TimeSpan timeout, CancellationToken parentToken)
        {
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(parentToken, timeoutCts.Token);

            using LogContext logContext = new(Logger.LogLevel.Debug, _adbInstance.LogHeader);

            string itemAsString = item switch
            {
                BourseItem.TreasureMap => "Fragment",
                BourseItem.CoffeeBean => "Grains",
                BourseItem.Katana => "Katana",
                BourseItem.Potion => "Potion",
                _ => throw new NotImplementedException()
            };

            List<string> queries = new()
            {
                "//node[@resource-id='contain-paidtasks-survey']",
                "//node[@text=\"Pas d'espace disponible dans l'entrepôt\"]",
                $"//node[@resource-id='com.ucdevs.jcross:id/clickItem'][descendant::node[@resource-id='com.ucdevs.jcross:id/tvSellName' and contains(@text, '{itemAsString}')] and descendant::node[@resource-id='com.ucdevs.jcross:id/tvBuyName' and @text='Regarder la pub']]"
            };

            while (true)
            {
                linkedCts.Token.ThrowIfCancellationRequested();

                FoundElement? foundElement = await Utils.FindElementAsync(_adbInstance, queries, TimeSpan.FromSeconds(2), linkedCts.Token);
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
                        Logger.Log(Logger.LogLevel.Info, _adbInstance.LogHeader, $"Clicking on {item}");
                        await foundElement.Element.ClickAsync(linkedCts.Token);
                        break;
                    default:
                        throw new Exception("Unexpected element index");
                }
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

        private class NoRoomForStorageException : Exception
        {
            public NoRoomForStorageException()
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
            await StartAsync(BourseItem.Potion, ActionWhenFull.Continue);
            await StartAsync(BourseItem.CoffeeBean, ActionWhenFull.Continue);
            await StartAsync(BourseItem.TreasureMap, ActionWhenFull.Stop);
        }
    }
}