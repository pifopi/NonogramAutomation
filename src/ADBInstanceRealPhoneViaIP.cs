namespace NonogramAutomation
{
    public class ADBInstanceRealPhoneViaIP : ADBInstanceViaIP
    {
        public override string LogHeader
        {
            get => $"{Name} | {IP}:{Port}";
        }

        public override Task StartEmulator(CancellationToken token)
        {
            using LogContext logContext = new(Logger.LogLevel.Info, LogHeader);
            return Task.CompletedTask;
        }

        public override Task StopEmulator()
        {
            using LogContext logContext = new(Logger.LogLevel.Info, LogHeader);
            return Task.CompletedTask;
        }
    }
}
