namespace NonogramAutomation
{
    public class UndoActions : IAsyncDisposable
    {
        private readonly List<Func<ValueTask>> _undoActions = new();

        public void Add(Func<ValueTask> undoAction)
        {
            _undoActions.Add(undoAction);
        }

        public async ValueTask DisposeAsync()
        {
            _undoActions.Reverse();
            foreach (var disposeAsync in _undoActions)
            {
                await disposeAsync();
            }
        }
    }
}
