using AdvancedSharpAdbClient.DeviceCommands;

namespace NonogramAutomation
{
    public class ProgramFavorite : Program
    {
        public ProgramFavorite(ADBInstance adbInstance, CancellationToken token)
             : base(adbInstance, token)
        {
        }

        private class PuzzleRecord
        {
            [CsvHelper.Configuration.Attributes.Name("Puzzle ID:Puzzle Name")]
            public required string Name { get; set; }
        }

        public override async Task StartAsync()
        {
            List<string> puzzles = GetAllPuzzles();

            try
            {
                await _adbInstance.ConnectToInstanceAsync(_token);

                int i = 0;
                foreach (string puzzle in puzzles)
                {
                    Logger.Log(Logger.LogLevel.Info, _adbInstance.LogHeader, $"Processing puzzle:{puzzle} {i}/{puzzles.Count}");
                    i++;

                    await GoToSearchMenuAsync(TimeSpan.FromSeconds(10), _token);
                    await InputPuzzleAsync(TimeSpan.FromSeconds(10), _token, puzzle);
                    await GoToPuzzleListAsync(TimeSpan.FromSeconds(10), _token);
                    await GoToPuzzleDetailsMenuAsync(TimeSpan.FromSeconds(10), _token);

                    bool isAlreadyFavorite = await Utils.DetectElementAsync(_adbInstance, "//node[@resource-id='com.ucdevs.jcross:id/removeFavorites']", TimeSpan.FromSeconds(2), _token);
                    if (isAlreadyFavorite)
                    {
                        Logger.Log(Logger.LogLevel.Info, _adbInstance.LogHeader, "Already favorite, skipping to next puzzle");
                        await Utils.ClickBackButtonAsync(_adbInstance, _token);
                        continue;
                    }

                    Logger.Log(Logger.LogLevel.Info, _adbInstance.LogHeader, "Adding to favorite");
                    await FavoritePuzzleAsync(TimeSpan.FromSeconds(10), _token);
                }
                Logger.Log(Logger.LogLevel.Info, _adbInstance.LogHeader, $"<@{SettingsManager.GlobalSettings.DiscordUserId}> Done processing all puzzles");
            }
            catch (Exception exception)
            {
                Logger.Log(Logger.LogLevel.Warning, _adbInstance.LogHeader, $"<@{SettingsManager.GlobalSettings.DiscordUserId}> An exception has been raised:{exception}");
            }
        }

        private List<string> GetAllPuzzles()
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, _adbInstance.LogHeader);

            var directoryInfo = new System.IO.DirectoryInfo("config");

            List<string> puzzles = new();
            foreach (var fileInfo in directoryInfo.EnumerateFiles("*.csv"))
            {
                puzzles.AddRange(GetPuzzlesFromCsv(fileInfo));
            }
            return puzzles;
        }

        private List<string> GetPuzzlesFromCsv(System.IO.FileInfo csvFile)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, _adbInstance.LogHeader);

            Logger.Log(Logger.LogLevel.Info, _adbInstance.LogHeader, $"Reading csv file : {csvFile.FullName}");

            var config = new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture);
            using (var reader = new System.IO.StreamReader(csvFile.FullName))
            using (var csv = new CsvHelper.CsvReader(reader, config))
            {
                return csv.GetRecords<PuzzleRecord>().Select(r => r.Name).ToList();
            }
        }

        private async Task GoToSearchMenuAsync(TimeSpan timeout, CancellationToken token)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, _adbInstance.LogHeader);

            await Utils.ClickElementAsync(_adbInstance, "//node[@resource-id='com.ucdevs.jcross:id/action_filter']", timeout, token);
        }

        private async Task InputPuzzleAsync(TimeSpan timeout, CancellationToken parentToken, string puzzle)
        {
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(parentToken, timeoutCts.Token);

            using LogContext logContext = new(Logger.LogLevel.Debug, _adbInstance.LogHeader);

            await Utils.ClickElementAsync(_adbInstance, "//node[@resource-id='com.ucdevs.jcross:id/editName']", timeout, linkedCts.Token);
            await _adbInstance.AdbClient.ClearInputAsync(_adbInstance.DeviceData, 10, linkedCts.Token);
            string puzzleId = System.Text.RegularExpressions.Regex.Replace(puzzle, @":.*(?=\])", "");
            await _adbInstance.AdbClient.SendTextAsync(_adbInstance.DeviceData, puzzleId, linkedCts.Token);
            await _adbInstance.AdbClient.ClickBackButtonAsync(_adbInstance.DeviceData, linkedCts.Token);
        }

        private async Task GoToPuzzleListAsync(TimeSpan timeout, CancellationToken token)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, _adbInstance.LogHeader);

            await Utils.ClickElementAsync(_adbInstance, "//node[@resource-id='com.ucdevs.jcross:id/buttonsHolder']/node[3]", timeout, token);
        }

        private async Task GoToPuzzleDetailsMenuAsync(TimeSpan timeout, CancellationToken token)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, _adbInstance.LogHeader);

            await Utils.ClickElementAsync(_adbInstance, "//node[@resource-id='com.ucdevs.jcross:id/btnCtxMenu']", timeout, token);
        }

        private async Task FavoritePuzzleAsync(TimeSpan timeout, CancellationToken token)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, _adbInstance.LogHeader);

            await Utils.ClickElementAsync(_adbInstance, "//node[@resource-id='com.ucdevs.jcross:id/addFavorites']", timeout, token);
        }
    }
}