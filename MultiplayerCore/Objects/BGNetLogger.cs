using SiraUtil.Logging;

namespace MultiplayerCore.Objects
{
    public class BGNetLogger : BGNetDebug.ILogger
    {
        private readonly SiraLog _logger;

        internal BGNetLogger(
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
