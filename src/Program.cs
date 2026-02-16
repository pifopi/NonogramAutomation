namespace NonogramAutomation
{
    public abstract class Program
    {
        protected ADBInstance _adbInstance;
        protected CancellationToken _token;

        protected Program(ADBInstance adbInstance, CancellationToken token)
        {
            _adbInstance = adbInstance;
            _token = token;
        }

        public abstract Task StartAsync();
    }
}