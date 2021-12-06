using SiraUtil.Logging;

namespace MultiplayerCore.Objects
{
    public class BGNetDebugLogger : BGNetDebug.ILogger
    {
        private readonly SiraLog _logger;

        internal BGNetDebugLogger(
            SiraLog logger)
        {
            _logger = logger;
            BGNetDebug.SetLogger(this);
        }

        public void LogError(string message)
            => _logger.Error(message);

        public void LogInfo(string message)
            => _logger.Info(message);

        public void LogWarning(string message)
            => _logger.Warn(message);
    }
}
