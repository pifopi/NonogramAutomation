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

        public class PuzzleRecord
        {
            [CsvHelper.Configuration.Attributes.Name("Puzzle_ID:Puzzle_name")]
            public required string Name { get; set; }
        }

        public async Task StartFavoritesAsync()
        {
            List<string> puzzles = new();

            var config = new CsvHelper.Configuration.CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture);
            using (var reader = new System.IO.StreamReader("config/table.csv"))
            using (var csv = new CsvHelper.CsvReader(reader, config))
            {
                puzzles = csv.GetRecords<PuzzleRecord>().Select(r => r.Name).ToList();
            }

            try
            {
                await using UndoActions undoActions = new();

                await ConnectToInstanceAsync(_programCts.Token);
                undoActions.Add(async () => await DisconnectFromInstanceAsync());

                foreach (string puzzle in puzzles)
                {
                    await GoToSearchMenuAsync(TimeSpan.FromSeconds(10), _programCts.Token);
                    await InputPuzzleAsync(TimeSpan.FromSeconds(10), _programCts.Token, puzzle);
                    await GoToPuzzleListAsync(TimeSpan.FromSeconds(10), _programCts.Token);

                    OpenCvSharp.Mat image = await Utils.Utils.GetImageAsync(_adbClient, _deviceData, TimeSpan.FromSeconds(10), _programCts.Token);
                    var searchFavoriteIconResult = ImageProcessing.SearchFavoriteIcon(image);
                    if (searchFavoriteIconResult.HasValue)
                    {
                        Logger.Log(Logger.LogLevel.Info, LogHeader, "Found favorite icon, skipping to next puzzle");
                        continue;
                    }

                    await GoToPuzzleDetailsMenuAsync(TimeSpan.FromSeconds(10), _programCts.Token);
                    await FavoritePuzzleAsync(TimeSpan.FromSeconds(10), _programCts.Token);
                }
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

        private async Task GoToSearchMenuAsync(TimeSpan timeout, CancellationToken parentToken)
        {
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(parentToken, timeoutCts.Token);

            using LogContext logContext = new(Logger.LogLevel.Debug, LogHeader);

            while (true)
            {
                linkedCts.Token.ThrowIfCancellationRequested();

                OpenCvSharp.Mat image = await Utils.Utils.GetImageAsync(_adbClient, _deviceData, TimeSpan.FromSeconds(10), linkedCts.Token);
                if (ImageProcessing.DetectSearchMenu(image))
                {
                    Logger.Log(Logger.LogLevel.Info, LogHeader, "Found search menu");
                    return;
                }

                var searchFunnelButtonResult = ImageProcessing.SearchFunnelButton(image);
                if (searchFunnelButtonResult.HasValue)
                {
                    (double alpha, System.Drawing.Point location) = searchFunnelButtonResult.Value;
                    Logger.Log(Logger.LogLevel.Info, LogHeader, $"Clicking on location:{location} (alpha:{alpha})");
                    await _adbClient.ClickAsync(_deviceData, location, linkedCts.Token);
                    await Task.Delay(TimeSpan.FromMilliseconds(100), linkedCts.Token);
                }
                await Task.Delay(TimeSpan.FromMilliseconds(100), linkedCts.Token);
            }
        }

        private async Task InputPuzzleAsync(TimeSpan timeout, CancellationToken parentToken, string puzzle)
        {
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(parentToken, timeoutCts.Token);

            using LogContext logContext = new(Logger.LogLevel.Debug, LogHeader);

            await _adbClient.ClearInputAsync(_deviceData, 10, _programCts.Token);
            string puzzleId = System.Text.RegularExpressions.Regex.Replace(puzzle, @":.*(?=\])", "");
            await _adbClient.SendTextAsync(_deviceData, puzzleId, _programCts.Token);
            await _adbClient.ClickBackButtonAsync(_deviceData, _programCts.Token);
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

        private async Task GoToPuzzleDetailsMenuAsync(TimeSpan timeout, CancellationToken parentToken)
        {
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(parentToken, timeoutCts.Token);

            using LogContext logContext = new(Logger.LogLevel.Debug, LogHeader);

            while (true)
            {
                linkedCts.Token.ThrowIfCancellationRequested();

                OpenCvSharp.Mat image = await Utils.Utils.GetImageAsync(_adbClient, _deviceData, TimeSpan.FromSeconds(10), linkedCts.Token);
                if (ImageProcessing.DetectPuzzleDetailsMenu(image))
                {
                    Logger.Log(Logger.LogLevel.Info, LogHeader, "Found puzzle details menu");
                    return;
                }

                var searchDetailsButtonResult = ImageProcessing.SearchDetailsButton(image);
                if (searchDetailsButtonResult.HasValue)
                {
                    (double alpha, System.Drawing.Point location) = searchDetailsButtonResult.Value;
                    Logger.Log(Logger.LogLevel.Info, LogHeader, $"Clicking on location:{location} (alpha:{alpha})");
                    await _adbClient.ClickAsync(_deviceData, location, linkedCts.Token);
                    await Task.Delay(TimeSpan.FromMilliseconds(100), linkedCts.Token);
                }
                await Task.Delay(TimeSpan.FromMilliseconds(100), linkedCts.Token);
            }
        }

        private async Task FavoritePuzzleAsync(TimeSpan timeout, CancellationToken parentToken)
        {
            using var timeoutCts = new CancellationTokenSource(timeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(parentToken, timeoutCts.Token);

            using LogContext logContext = new(Logger.LogLevel.Debug, LogHeader);

            while (true)
            {
                linkedCts.Token.ThrowIfCancellationRequested();

                OpenCvSharp.Mat image = await Utils.Utils.GetImageAsync(_adbClient, _deviceData, TimeSpan.FromSeconds(10), linkedCts.Token);
                //TODO no detection here as it's not always the same screen
                //if (ImageProcessing.DetectPuzzleListMenu(image))
                //{
                //    Logger.Log(Logger.LogLevel.Info, LogHeader, "Found puzzle list menu");
                //    return;
                //}

                var searchFavoriteButtonResult = ImageProcessing.SearchFavoriteButton(image);
                if (searchFavoriteButtonResult.HasValue)
                {
                    (double alpha, System.Drawing.Point location) = searchFavoriteButtonResult.Value;
                    Logger.Log(Logger.LogLevel.Info, LogHeader, $"Clicking on location:{location} (alpha:{alpha})");
                    await _adbClient.ClickAsync(_deviceData, location, linkedCts.Token);
                    await Task.Delay(TimeSpan.FromMilliseconds(100), linkedCts.Token);
                    return;//TODO don't return here
                }
                await Task.Delay(TimeSpan.FromMilliseconds(100), linkedCts.Token);
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
