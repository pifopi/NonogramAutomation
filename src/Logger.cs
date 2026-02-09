namespace NonogramAutomation
{
    public static class Logger
    {
        public enum LogLevel
        {
            Warning,
            Info,
            Debug
        }

        private static readonly NLog.Logger _logger = NLog.LogManager.GetLogger("");

        public static void Log(LogLevel logLevel, string header, string message, [System.Runtime.CompilerServices.CallerMemberName] string methodName = "")
        {
            var fullMessage = string.IsNullOrEmpty(message) ? $"{header} | {methodName}" : $"{header} | {methodName} | {message}";
            switch (logLevel)
            {
                case LogLevel.Warning:
                    _logger.Warn(fullMessage);
                    break;
                case LogLevel.Info:
                    _logger.Info(fullMessage);
                    break;
                case LogLevel.Debug:
                    _logger.Debug(fullMessage);
                    break;
            }
        }
    }

    public sealed class LogContext : IDisposable
    {
        private readonly Logger.LogLevel _logLevel;
        private readonly string _header;
        private readonly string _methodName;

        public LogContext(Logger.LogLevel logLevel, string header, [System.Runtime.CompilerServices.CallerMemberName] string methodName = "")
        {
            Logger.Log(logLevel, header, "", $"{methodName} Begin");
            _logLevel = logLevel;
            _header = header;
            _methodName = methodName;
        }

        public void Dispose()
        {
            Logger.Log(_logLevel, _header, "", $"{_methodName} End");
        }
    }

    [NLog.Targets.Target("Discord")]
    public class DiscordLogger : NLog.Targets.AsyncTaskTarget
    {
        private static readonly Discord.WebSocket.DiscordSocketClient _client = new();
        private static readonly List<(NLog.LogEventInfo, CancellationToken)> _waitingMessages = [];

        protected override async Task WriteAsyncTask(NLog.LogEventInfo logEvent, CancellationToken cancellationToken)
        {
            switch (_client.LoginState)
            {
                case Discord.LoginState.LoggingIn:
                case Discord.LoginState.LoggingOut:
                    _waitingMessages.Add((logEvent, cancellationToken));
                    break;
                case Discord.LoginState.LoggedOut:
                    _waitingMessages.Add((logEvent, cancellationToken));
                    await _client.LoginAsync(Discord.TokenType.Bot, SettingsManager.GetDiscordBotToken());
                    await _client.StartAsync();
                    _client.Ready += SendWaitingMessagesAsync;
                    break;
                case Discord.LoginState.LoggedIn:
                    if (await _client.GetChannelAsync(SettingsManager.GlobalSettings.DiscordChannelId) is Discord.IMessageChannel channel)
                    {
                        await channel.SendMessageAsync(logEvent.Message);
                    }
                    break;
            }
        }

        private async Task SendWaitingMessagesAsync()
        {
            foreach ((NLog.LogEventInfo logEvent, CancellationToken cancellationToken) in _waitingMessages)
            {
                await WriteAsyncTask(logEvent, cancellationToken);
            }
            _waitingMessages.Clear();
        }
    }
}
