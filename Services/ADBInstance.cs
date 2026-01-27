using AdvancedSharpAdbClient.DeviceCommands;
using NonogramAutomation.Enums;
using NonogramAutomation.Models;

namespace NonogramAutomation.Services
{
    public abstract class ADBInstance : System.ComponentModel.INotifyPropertyChanged
    {
        private string _name = "New instance";
        private InstanceStatus _status = InstanceStatus.Idle;
        private ProgramType _selectedProgram = ProgramType.Favorites;

        protected AdvancedSharpAdbClient.AdbClient _adbClient = new();
        protected AdvancedSharpAdbClient.Models.DeviceData _deviceData = new();

        private CancellationTokenSource _programCts = new();

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }

        public InstanceStatus Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsRunning));
                OnPropertyChanged(nameof(IsIdle));
            }
        }

        public bool IsRunning
        {
            get => Status == InstanceStatus.Running;
        }

        public bool IsIdle
        {
            get => Status == InstanceStatus.Idle;
        }

        public static System.Collections.ObjectModel.ObservableCollection<ProgramType> ProgramList { get; set; } = new(Enum.GetValues(typeof(ProgramType)).Cast<ProgramType>());

        public ProgramType SelectedProgram
        {
            get => _selectedProgram;
            set { _selectedProgram = value; OnPropertyChanged(); }
        }

        protected abstract string LogHeader { get; }

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }

        public void StartProgram()
        {
            using LogContext logContext = new(Logger.LogLevel.Info, LogHeader, $"StartProgram {SelectedProgram}");
            Status = InstanceStatus.Running;
            _programCts = new CancellationTokenSource();
            Func<Task> task = SelectedProgram switch
            {
                ProgramType.Favorites => StartFavoritesAsync,
                ProgramType.Bourse => StartBourseAsync,
                _ => throw new NotImplementedException()
            };
            Task.Run(task).ContinueWith(t => Status = InstanceStatus.Idle);
        }

        public void StopProgram()
        {
            using LogContext logContext = new(Logger.LogLevel.Info, LogHeader, $"StopProgram {SelectedProgram}");
            _programCts.Cancel();
            _programCts.Dispose();
            Status = InstanceStatus.Stopping;
        }

        protected abstract Task ConnectToInstanceAsync(CancellationToken token);

        protected abstract Task DisconnectFromInstanceAsync();

        private async Task ClickElementByResourceIdAsync(
                AdvancedSharpAdbClient.AdbClient adbClient,
                AdvancedSharpAdbClient.Models.DeviceData deviceData,
                string resourceId,
                TimeSpan timeout,
                CancellationToken parentToken
            )
        {
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(parentToken, timeoutCts.Token);

            Logger.Log(Logger.LogLevel.Info, LogHeader, $"Searching for {resourceId}");

            while (true)
            {
                linkedCts.Token.ThrowIfCancellationRequested();

                AdvancedSharpAdbClient.DeviceCommands.Models.Element? element = await Utils.Utils.FindElementByResourceIdAsync(adbClient, deviceData, resourceId, linkedCts.Token);
                if (element != null)
                {
                    Logger.Log(Logger.LogLevel.Info, LogHeader, $"Clicking on {resourceId}");
                    await element.ClickAsync(linkedCts.Token);
                    await Task.Delay(TimeSpan.FromMilliseconds(100), linkedCts.Token);
                    return;
                }
            }
        }

        public class PuzzleRecord
        {
            [CsvHelper.Configuration.Attributes.Name("Puzzle ID:Puzzle Name")]
            public required string Name { get; set; }
        }

        public async Task StartFavoritesAsync()
        {
            List<string> puzzles = GetAllPuzzles();

            try
            {
                await ConnectToInstanceAsync(_programCts.Token);

                int i = 0;
                foreach (string puzzle in puzzles)
                {
                    Logger.Log(Logger.LogLevel.Info, LogHeader, $"Processing puzzle:{puzzle} {i}/{puzzles.Count}");
                    i++;

                    await GoToSearchMenuAsync(TimeSpan.FromSeconds(10), _programCts.Token);
                    await InputPuzzleAsync(TimeSpan.FromSeconds(10), _programCts.Token, puzzle);
                    await GoToPuzzleListAsync(TimeSpan.FromSeconds(10), _programCts.Token);
                    await GoToPuzzleDetailsMenuAsync(TimeSpan.FromSeconds(10), _programCts.Token);

                    bool isAlreadyFavorite = await Utils.Utils.DetectElementByResourceIdAsync(_adbClient, _deviceData, "com.ucdevs.jcross:id/removeFavorites", TimeSpan.FromSeconds(2), _programCts.Token);
                    if (isAlreadyFavorite)
                    {
                        Logger.Log(Logger.LogLevel.Info, LogHeader, "Found favorite icon, skipping to next puzzle");
                        await _adbClient.ClickBackButtonAsync(_deviceData, _programCts.Token);
                        continue;
                    }

                    Logger.Log(Logger.LogLevel.Info, LogHeader, "Adding to favorite");
                    await FavoritePuzzleAsync(TimeSpan.FromSeconds(10), _programCts.Token);
                }
                Logger.Log(Logger.LogLevel.Info, LogHeader, $"<@{SettingsManager.GlobalSettings.DiscordUserId}> Done processing all puzzles");
                return;
            }
            catch (OperationCanceledException exception)
            {
                Logger.Log(Logger.LogLevel.Info, LogHeader, $"An exception has been raised:{exception}");
            }
            catch (Exception exception)
            {
                Logger.Log(Logger.LogLevel.Warning, LogHeader, $"<@{SettingsManager.GlobalSettings.DiscordUserId}> An exception has been raised:{exception}");
            }
        }

        private List<string> GetPuzzlesFromCsv(System.IO.FileInfo csvFile)
        {
            Logger.Log(Logger.LogLevel.Info, LogHeader, $"Reading csv file : {csvFile.FullName}");

            var config = new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture);
            using (var reader = new System.IO.StreamReader(csvFile.FullName))
            using (var csv = new CsvHelper.CsvReader(reader, config))
            {
                return csv.GetRecords<PuzzleRecord>().Select(r => r.Name).ToList();
            }
        }

        private List<string> GetAllPuzzles()
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, LogHeader);

            var directoryInfo = new System.IO.DirectoryInfo("config");

            List<string> puzzles = new();
            foreach (var fileInfo in directoryInfo.EnumerateFiles("*.csv"))
            {
                puzzles.AddRange(GetPuzzlesFromCsv(fileInfo));
            }
            return puzzles;
        }

        private async Task GoToSearchMenuAsync(TimeSpan timeout, CancellationToken token)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, LogHeader);

            await ClickElementByResourceIdAsync(_adbClient, _deviceData, "com.ucdevs.jcross:id/action_filter", timeout, token);
        }

        private async Task InputPuzzleAsync(TimeSpan timeout, CancellationToken parentToken, string puzzle)
        {
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(parentToken, timeoutCts.Token);

            using LogContext logContext = new(Logger.LogLevel.Debug, LogHeader);

            await _adbClient.ClearInputAsync(_deviceData, 10, linkedCts.Token);
            string puzzleId = System.Text.RegularExpressions.Regex.Replace(puzzle, @":.*(?=\])", "");
            await _adbClient.SendTextAsync(_deviceData, puzzleId, linkedCts.Token);
            await _adbClient.ClickBackButtonAsync(_deviceData, linkedCts.Token);
        }

        private async Task GoToPuzzleListAsync(TimeSpan timeout, CancellationToken parentToken)
        {
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(parentToken, timeoutCts.Token);

            using LogContext logContext = new(Logger.LogLevel.Debug, LogHeader);

            while (true)
            {
                linkedCts.Token.ThrowIfCancellationRequested();

                OpenCvSharp.Mat image = await Utils.Utils.GetImageAsync(_adbClient, _deviceData, TimeSpan.FromSeconds(10), linkedCts.Token);
                if (ImageProcessing.DetectPuzzleListMenu(image))
                {
                    Logger.Log(Logger.LogLevel.Info, LogHeader, "Found puzzle list menu");
                    return;
                }

                var searchPlayButtonResult = ImageProcessing.SearchPlayButton(image);
                if (searchPlayButtonResult.HasValue)
                {
                    (double alpha, System.Drawing.Point location) = searchPlayButtonResult.Value;
                    Logger.Log(Logger.LogLevel.Info, LogHeader, $"Clicking on location:{location} (alpha:{alpha})");
                    await _adbClient.ClickAsync(_deviceData, location, linkedCts.Token);
                    await Task.Delay(TimeSpan.FromMilliseconds(100), linkedCts.Token);
                }
                await Task.Delay(TimeSpan.FromMilliseconds(100), linkedCts.Token);
            }
        }

        private async Task GoToPuzzleDetailsMenuAsync(TimeSpan timeout, CancellationToken token)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, LogHeader);

            await ClickElementByResourceIdAsync(_adbClient, _deviceData, "com.ucdevs.jcross:id/btnCtxMenu", timeout, token);
        }

        private async Task FavoritePuzzleAsync(TimeSpan timeout, CancellationToken token)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, LogHeader);

            await ClickElementByResourceIdAsync(_adbClient, _deviceData, "com.ucdevs.jcross:id/addFavorites", timeout, token);
        }

        private enum BourseItem
        {
            Carte,
            Café,
            Katana,
            Potion
        }

        public async Task StartBourseAsync()
        {

            try
            {
                await ConnectToInstanceAsync(_programCts.Token);

                await GoToBourseAsync(TimeSpan.FromSeconds(10), _programCts.Token);
                await ScrollAndClickOnItemAsync(BourseItem.Katana, TimeSpan.FromSeconds(30), _programCts.Token);
            }
            catch (OperationCanceledException exception)
            {
                Logger.Log(Logger.LogLevel.Info, LogHeader, $"An exception has been raised:{exception}");
            }
            catch (Exception exception)
            {
                Logger.Log(Logger.LogLevel.Warning, LogHeader, $"<@{SettingsManager.GlobalSettings.DiscordUserId}> An exception has been raised:{exception}");
            }
        }

        private async Task GoToBourseAsync(TimeSpan timeout, CancellationToken token)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, LogHeader);

            await ClickElementByResourceIdAsync(_adbClient, _deviceData, "com.ucdevs.jcross:id/catBourse", timeout, token);
        }

        private async Task ScrollAndClickOnItemAsync(BourseItem item, TimeSpan timeout, CancellationToken parentToken)
        {
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(parentToken, timeoutCts.Token);

            using LogContext logContext = new(Logger.LogLevel.Debug, LogHeader);

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

                using var searchTimeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(2000));
                using var searchLinkedCts = CancellationTokenSource.CreateLinkedTokenSource(linkedCts.Token, searchTimeoutCts.Token);

                AdvancedSharpAdbClient.DeviceCommands.Models.Element? element = await _adbClient.FindElementAsync(_deviceData, query, searchLinkedCts.Token);
                if (element is not null)
                {
                    Logger.Log(Logger.LogLevel.Info, LogHeader, $"Clicking on {item}");
                    await element.ClickAsync(linkedCts.Token);
                    await Task.Delay(TimeSpan.FromMilliseconds(100), linkedCts.Token);
                    return;
                }

                Logger.Log(Logger.LogLevel.Info, LogHeader, $"Scrolling to find {item}");
                await _adbClient.SwipeAsync(_deviceData, new System.Drawing.Point(500, 1500), new System.Drawing.Point(500, 500), 500, linkedCts.Token);
            }
        }
    }

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

            string resultConnect = await _adbClient.ConnectAsync(IP, Port, token);
            if (resultConnect != $"connected to {IP}:{Port}" &&
                resultConnect != $"already connected to {IP}:{Port}")
            {
                throw new Exception(resultConnect);
            }
            _deviceData = await Utils.Utils.GetDeviceDataFromAsync(_adbClient, $"{IP}:{Port}", TimeSpan.FromMinutes(1), token);
        }

        protected override async Task DisconnectFromInstanceAsync()
        {
            try
            {
                await _adbClient.DisconnectAsync(IP, Port);
            }
            catch (Exception exception)
            {
                Logger.Log(Logger.LogLevel.Warning, LogHeader, $"<@{SettingsManager.GlobalSettings.DiscordUserId}> An exception has been raised:{exception}");
            }
            finally
            {
                _deviceData = new();
            }
        }

        protected virtual int GetPort()
        {
            return Port;
        }
    }

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
            _deviceData = await Utils.Utils.GetDeviceDataFromAsync(_adbClient, SerialName, TimeSpan.FromMinutes(1), token);
        }

        protected override Task DisconnectFromInstanceAsync()
        {
            _deviceData = new();
            return Task.CompletedTask;
        }
    }
}
