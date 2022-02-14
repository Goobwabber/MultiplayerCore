using SiraUtil.Affinity;
using SiraUtil.Logging;

namespace MultiplayerCore.Patchers
{
    public class DebugPatcher : IAffinity
    {
        private readonly SiraLog _logger;

        public DebugPatcher(SiraLog logger)
        {
            _logger = logger;
        }
    }
}
