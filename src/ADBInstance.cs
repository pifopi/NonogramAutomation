using AdvancedSharpAdbClient.DeviceCommands;

namespace NonogramAutomation
{
    public abstract class ADBInstance : System.ComponentModel.INotifyPropertyChanged
    {
        private string _name = "New instance";
        private InstanceStatus _status = InstanceStatus.Idle;
        private ProgramType _selectedProgram = ProgramType.Dump;

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

        public abstract string LogHeader { get; }

        public AdvancedSharpAdbClient.AdbClient AdbClient { get; } = new();

        public AdvancedSharpAdbClient.Models.DeviceData DeviceData { get; set; } = new();


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
            Program program = SelectedProgram switch
            {
                ProgramType.Dump => new ProgramDump(this, _programCts.Token),
                ProgramType.Bourse => new ProgramBourse(this, _programCts.Token),
                ProgramType.Favorites => new ProgramFavorite(this, _programCts.Token),
                ProgramType.Download => new ProgramDownload(this, _programCts.Token),
                _ => throw new NotImplementedException()
            };
            Task.Run(program.StartAsync).ContinueWith(t => Status = InstanceStatus.Idle);
        }

        public void StopProgram()
        {
            using LogContext logContext = new(Logger.LogLevel.Info, LogHeader, $"StopProgram {SelectedProgram}");
            _programCts.Cancel();
            _programCts.Dispose();
            Status = InstanceStatus.Stopping;
        }

        public abstract Task StartEmulator(CancellationToken token);

        public abstract Task StopEmulator();

        public abstract Task ConnectToInstanceAsync(CancellationToken token);

        public abstract Task DisconnectFromInstanceAsync();

        public async Task StartApplicationAsync(CancellationToken token)
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, LogHeader);
            await AdbClient.StartAppAsync(DeviceData, "com.ucdevs.jcross", token);
        }

        public async Task StopApplicationAsync()
        {
            using LogContext logContext = new(Logger.LogLevel.Debug, LogHeader);
            await AdbClient.StopAppAsync(DeviceData, "com.ucdevs.jcross");
        }
    }
}
