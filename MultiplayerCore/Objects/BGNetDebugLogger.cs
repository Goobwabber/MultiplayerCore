﻿using System;
using BGNet.Logging;
using SiraUtil.Logging;

namespace MultiplayerCore.Objects
{
    // TODO: Turn into patches, this has never worked
    internal class BGNetDebugLogger : Debug.ILogger
    {
        private readonly SiraLog _logger;

        internal BGNetDebugLogger(
            SiraLog logger)
        {
            _logger = logger;
            Debug.AddLogger(this);
        }

        public void LogError(string message)
            => _logger.Error(message);

        public void LogException(Exception exception, string? message = null)
        {
            if (message != null)
                _logger.Error(message);
            _logger.Error(exception);
        }

        public void LogInfo(string message)
            => _logger.Info(message);

        public void LogWarning(string message)
            => _logger.Warn(message);
    }
}
