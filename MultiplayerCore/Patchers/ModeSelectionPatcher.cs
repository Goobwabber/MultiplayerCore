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
        [AffinityPatch(typeof(MasterServerAvailabilityModel), nameof(MasterServerAvailabilityModel.IsAvailabilityTaskValid))]
        private bool IsAvailabilityTaskValid(ref bool __result)
        {
            if (_networkConfig.masterServerStatusUrl == _lastStatusUrl)
                return true;
            _lastStatusUrl = _networkConfig.masterServerStatusUrl;
            __result = false;
            return false;
        }

        [AffinityPrefix]
        [AffinityPatch(typeof(MasterServerQuickPlaySetupModel), nameof(MasterServerQuickPlaySetupModel.IsQuickPlaySetupTaskValid))]
        private bool IsQuickplaySetupTaskValid(ref bool __result)
        {
            if (_networkConfig.masterServerStatusUrl == _lastStatusUrl)
                return true;
            _lastStatusUrl = _networkConfig.masterServerStatusUrl;
            __result = false;
            return false;
        }

        // If there is no availability data, assume that it's fine
        [AffinityPrefix]
        [AffinityPatch(typeof(MultiplayerUnavailableReasonMethods), nameof(MultiplayerUnavailableReasonMethods.TryGetMultiplayerUnavailableReason))]
        private bool GetMultiplayerUnavailableReason(MasterServerAvailabilityData data, ref bool __result)
        {
            if (data != null)
                return true;
            __result = false;
            return false;
        }
    }
}
