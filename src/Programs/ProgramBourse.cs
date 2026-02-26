namespace NonogramAutomation
{
    public abstract class ProgramBourse : Program
    {
        private int itemFarmCount = 0;

        public ProgramBourse(ADBInstance adbInstance, CancellationToken token)
             : base(adbInstance, token)
        {
        }

        protected enum BourseItem
        {
            TreasureMap,
            Coffee,
            Katana,
            Potion
        }

        protected async Task StartAsync(BourseItem item)
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

                    List<string> mainMenuQueries = new()
                    {
                        "//node[@resource-id='com.ucdevs.jcross:id/btnGuild']",
                        "//node[@text='Warning: Guild last saved progress is not accessible, loaded from previous slot.']",
                        "//node[@text='Warning: Guild saved progress is not accessible']"
                    };
                    FoundElement? foundElement = await Utils.FindElementAsync(_adbInstance, mainMenuQueries, TimeSpan.FromSeconds(10), _token);
                    if (foundElement is null)
                    {
                        throw new Exception("Main menu not found");
                    }
                    switch (foundElement.Index)
                    {
                        case 0:
                            Logger.Log(Logger.LogLevel.Info, _adbInstance.LogHeader, $"Main menu found");
                            break;
                        case 1:
                            Logger.Log(Logger.LogLevel.Warning, _adbInstance.LogHeader, $"<@{SettingsManager.GlobalSettings.DiscordUserId}> Guild saved progress lost (low severity)");
                            await ReturnToMainMenuAsync(TimeSpan.FromSeconds(10), _token);
                            await LoadBackupAsync(withEmptySave: false);
                            break;
                        case 2:
                            Logger.Log(Logger.LogLevel.Warning, _adbInstance.LogHeader, $"<@{SettingsManager.GlobalSettings.DiscordUserId}> Guild saved progress lost (high severity)");
                            await ReturnToMainMenuAsync(TimeSpan.FromSeconds(10), _token);
                            await LoadBackupAsync(withEmptySave: true);
                            break;
                        default:
                            throw new Exception("Unexpected element index");
                    }
                    await ClickOnGuildAsync(TimeSpan.FromSeconds(10), _token);
                    await ClickOnBourseAsync(TimeSpan.FromSeconds(10), _token);
                    await ScrollAndClickOnItemAsync(item, TimeSpan.FromSeconds(30), _token);
                    try
                    {
                        await WaitForRewardAsync(TimeSpan.FromSeconds(80), _token);
                    }
                    catch (NoRoomForStorageException exception)
                    {
                        while (true)
                        {
                            Logger.Log(Logger.LogLevel.Info, _adbInstance.LogHeader, $"<@{SettingsManager.GlobalSettings.DiscordUserId}> An exception has been raised:{exception}");
                            await Task.Delay(TimeSpan.FromMinutes(1));
                        }
                    }

                    await ReturnToMainMenuAsync(TimeSpan.FromSeconds(60), _token);
                    await SaveBackupAsync();
                    await ReturnToMainMenuAsync(TimeSpan.FromSeconds(10), _token);
                }
                catch (Exception exception)
                {
                    Logger.Log(Logger.LogLevel.Warning, _adbInstance.LogHeader, $"<@{SettingsManager.GlobalSettings.DiscordUserId}> An exception has been raised:{exception}");
                }
            }
        }

        private async Task LoadBackupAsync(bool withEmptySave)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, _adbInstance.LogHeader);

            await ClickOnSettingsAsync(TimeSpan.FromSeconds(10), _token);
            await ClickOnOtherAsync(TimeSpan.FromSeconds(10), _token);
            await ClickOnLoadZipAsync(TimeSpan.FromSeconds(10), _token);
            await ClickOnLoadAsync(TimeSpan.FromSeconds(10), _token);
            if (withEmptySave)
            {
                await ClickOnOKAsync(TimeSpan.FromSeconds(10), _token);
            }
            await ReturnToMainMenuAsync(TimeSpan.FromSeconds(10), _token);
        }

        private async Task SaveBackupAsync()
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, _adbInstance.LogHeader);

            string folder = @"C:\Users\dotte\Documents\MuMuSharedFolder";
            string saveFilename = System.IO.Path.Combine(folder, "NonogramsKatana.zip");

            await ClickOnSettingsAsync(TimeSpan.FromSeconds(10), _token);
            await ClickOnOtherAsync(TimeSpan.FromSeconds(10), _token);
            await ClickOnSaveZipAsync(TimeSpan.FromSeconds(10), _token);

            System.IO.File.Delete(saveFilename);

            await ClickOnSaveAsync(TimeSpan.FromSeconds(10), _token);

            itemFarmCount++;
            string backupFilename = System.IO.Path.Combine(folder, "NonogramsKatanaBackups", $"{DateTime.Now:yyyyMMdd_HHmmss}_{itemFarmCount}.zip");
            System.IO.File.Copy(saveFilename, backupFilename);

            Logger.Log(Logger.LogLevel.Info, _adbInstance.LogHeader, $"New backup saved in {backupFilename}");
        }

        private async Task ClickOnGuildAsync(TimeSpan timeout, CancellationToken token)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, _adbInstance.LogHeader);

            await Utils.ClickElementAsync(_adbInstance, "//node[@resource-id='com.ucdevs.jcross:id/btnGuild']", timeout, token);
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

        private async Task ClickOnOKAsync(TimeSpan timeout, CancellationToken token)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, _adbInstance.LogHeader);

            await Utils.ClickElementAsync(_adbInstance, "//node[@text='OK']", timeout, token);
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
                    Logger.Log(Logger.LogLevel.Info, _adbInstance.LogHeader, $"Clicking on {item}");
                    await foundElement.Element.ClickAsync(linkedCts.Token);
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

        private async Task WaitForRewardAsync(TimeSpan timeout, CancellationToken parentToken)
        {
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(parentToken, timeoutCts.Token);

            using LogContext logContext = new(Logger.LogLevel.Debug, _adbInstance.LogHeader);

            List<string> queries = new()
            {
                "//node[@resource-id='contain-paidtasks-survey']",
                "//node[@text=\"Pas d'espace disponible dans l'entrepôt\"]",
                "//node[@class='android.view.View']"
            };

            FoundElement? foundElement = await Utils.FindElementAsync(_adbInstance, queries, TimeSpan.FromSeconds(30), linkedCts.Token);
            if (foundElement is null)
            {
                await Utils.DumpAllAsync(_adbInstance, "NoAds", false, linkedCts.Token);
                throw new Exception("No ad was loaded after 10s");
            }
            switch (foundElement.Index)
            {
                case 0:
                    throw new Exception("Survey detected");
                case 1:
                    throw new NoRoomForStorageException();
                case 2:
                    Logger.Log(Logger.LogLevel.Info, _adbInstance.LogHeader, $"Ad loaded properly");
                    break;
                default:
                    throw new Exception("Unexpected element index");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), linkedCts.Token);

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
            await StartAsync(BourseItem.TreasureMap);
        }
    }

    public class ProgramBourseCoffee : ProgramBourse
    {
        public ProgramBourseCoffee(ADBInstance adbInstance, CancellationToken token)
             : base(adbInstance, token)
        {
        }

        public override async Task StartAsync()
        {
            await StartAsync(BourseItem.Coffee);
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
            await StartAsync(BourseItem.Katana);
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
            await StartAsync(BourseItem.Potion);
        }
    }
}