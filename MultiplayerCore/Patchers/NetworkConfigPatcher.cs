using SiraUtil.Affinity;
using SiraUtil.Logging;

namespace MultiplayerCore.Patchers
{
    public class NetworkConfigPatcher : IAffinity
    {
        /// <summary>
        /// The <see cref="INetworkConfig"/> being used. 
        /// Uses Official config when null.
        /// </summary>
        public INetworkConfig? NetworkConfig => _networkConfig;

        private bool _enableOfficial = true;
        private INetworkConfig? _networkConfig = null;

        private readonly SiraLog _logger;

        internal NetworkConfigPatcher(
            SiraLog logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Uses the network config from the provided <see cref="INetworkConfig"/> object.
        /// </summary>
        /// <param name="networkConfig">The <see cref="INetworkConfig"/> to use</param>
        public void UseNetworkConfig(INetworkConfig networkConfig)
        {
            _networkConfig = networkConfig;
            _enableOfficial = false;
        }

        /// <summary>
        /// Uses the official network config.
        /// </summary>
        public void UseOfficialConfig()
        {
            _enableOfficial = true;
        }

        [AffinityPatch(typeof(NetworkConfigSO), nameof(NetworkConfigSO.masterServerEndPoint), AffinityMethodType.Getter)]
        private void GetMasterServerEndPoint(ref MasterServerEndPoint __result)
        {
            if (_enableOfficial || _networkConfig == null)
                return;

            __result = _networkConfig.masterServerEndPoint;
            _logger.Debug($"Patching master server end point with '{__result}'.");
        }

        [AffinityPatch(typeof(NetworkConfigSO), nameof(NetworkConfigSO.masterServerStatusUrl), AffinityMethodType.Getter)]
        private void GetMasterServerStatusUrl(ref string __result)
        {
            if (_enableOfficial || _networkConfig == null)
                return;

            __result = _networkConfig.masterServerStatusUrl;
            _logger.Debug($"Patching master server status URL with '{__result}'.");
        }

        [AffinityPatch(typeof(NetworkConfigSO), nameof(NetworkConfigSO.maxPartySize), AffinityMethodType.Getter)]
        private void GetMaxPartySize(ref int __result)
        {
            if (_enableOfficial || _networkConfig == null)
                return;

            __result = _networkConfig.maxPartySize;
            _logger.Debug($"Patching master server max party size with '{__result}'.");
        }

        [AffinityPatch(typeof(NetworkConfigSO), nameof(NetworkConfigSO.discoveryPort), AffinityMethodType.Getter)]
        private void GetDiscoveryPort(ref int __result)
        {
            if (_enableOfficial || _networkConfig == null)
                return;

            __result = _networkConfig.discoveryPort;
            _logger.Debug($"Patching master server discovery port with '{__result}'.");
        }

        [AffinityPatch(typeof(NetworkConfigSO), nameof(NetworkConfigSO.partyPort), AffinityMethodType.Getter)]
        private void GetPartyPort(ref int __result)
        {
            if (_enableOfficial || _networkConfig == null)
                return;

            __result = _networkConfig.partyPort;
            _logger.Debug($"Patching master server party port with '{__result}'.");
        }

        [AffinityPatch(typeof(NetworkConfigSO), nameof(NetworkConfigSO.multiplayerPort), AffinityMethodType.Getter)]
        private void GetMultiplayerPort(ref int __result)
        {
            if (_enableOfficial || _networkConfig == null)
                return;

            __result = _networkConfig.multiplayerPort;
            _logger.Debug($"Patching master server multiplayer port with '{__result}'.");
        }

        [AffinityPatch(typeof(UserCertificateValidator), "ValidateCertificateChainInternal")]
        private bool ValidateCertificateChain()
        {
            if (_enableOfficial || _networkConfig == null)
                return true;

            // TODO
            // It'd be best if we do certificate validation here...
            // but for now we'll just skip it.
            return false;
        }
    }
}
