namespace NonogramAutomation
{
    public class ProgramStartEmulator : Program
    {
        public ProgramStartEmulator(ADBInstance adbInstance, CancellationToken token)
             : base(adbInstance, token)
        {
        }

        public override async Task StartAsync()
        {
            await _adbInstance.StartEmulator(_token);
        }
    }
}