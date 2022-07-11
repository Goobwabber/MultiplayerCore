using SiraUtil.Affinity;

namespace MultiplayerCore.Patchers
{
    public class ModeSelectionPatcher : IAffinity
    {
        private string _lastStatusUrl = string.Empty;

        private readonly INetworkConfig _networkConfig;

        internal ModeSelectionPatcher(
            INetworkConfig networkConfig)
        {
            _networkConfig = networkConfig;
        }

        [AffinityPrefix]
        [AffinityPatch(typeof(MultiplayerStatusModel), nameof(MultiplayerStatusModel.IsAvailabilityTaskValid))]
        private bool IsAvailabilityTaskValid(ref bool __result)
        {
            if (_networkConfig.multiplayerStatusUrl == _lastStatusUrl)
                return true;
            _lastStatusUrl = _networkConfig.multiplayerStatusUrl;
            __result = false;
            return false;
        }

        [AffinityPrefix]
        [AffinityPatch(typeof(QuickPlaySetupModel), nameof(QuickPlaySetupModel.IsQuickPlaySetupTaskValid))]
        private bool IsQuickplaySetupTaskValid(ref bool __result)
        {
            if (_networkConfig.multiplayerStatusUrl == _lastStatusUrl)
                return true;
            _lastStatusUrl = _networkConfig.multiplayerStatusUrl;
            __result = false;
            return false;
        }

        // If there is no availability data, assume that it's fine
        [AffinityPrefix]
        [AffinityPatch(typeof(MultiplayerUnavailableReasonMethods), nameof(MultiplayerUnavailableReasonMethods.TryGetMultiplayerUnavailableReason))]
        private bool GetMultiplayerUnavailableReason(MultiplayerStatusData? data, ref bool __result)
        {
            if (data != null)
                return true;
            __result = false;
            return false;
        }
    }
}
