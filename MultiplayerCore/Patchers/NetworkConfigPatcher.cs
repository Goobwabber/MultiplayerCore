using SiraUtil.Affinity;
using SiraUtil.Logging;

namespace MultiplayerCore.Patchers
{
    public class NetworkConfigPatcher : IAffinity
    {
        public const int OfficialMaxPartySize = 5;

        public DnsEndPoint? MasterServerEndPoint { get; set; }
        public string? MasterServerStatusUrl { get; set; }
        public string? QuickPlaySetupUrl { get; set; }
        public int? MaxPartySize { get; set; }
        public int? DiscoveryPort { get; set; }
        public int? PartyPort { get; set; }
        public int? MultiplayerPort { get; set; }
        public bool DisableGameLift { get; set; }

        private readonly SiraLog _logger;

        internal NetworkConfigPatcher(
            SiraLog logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Uses a custom master server
        /// </summary>
        /// <param name="endPoint"></param>
        /// <param name="statusUrl"></param>
        /// <param name="maxPartySize"></param>
        public void UseMasterServer(DnsEndPoint endPoint, string statusUrl, int? maxPartySize = null)
        {
            _logger.Debug($"Master server set to '{endPoint}'");
            MasterServerEndPoint = endPoint;
            MasterServerStatusUrl = statusUrl;
            MaxPartySize = maxPartySize;
            QuickPlaySetupUrl = statusUrl + "/mp_override.json";
            DisableGameLift = true;
        }

        /// <summary>
        /// Uses a custom master server
        /// </summary>
        /// <param name="endPoint"></param>
        /// <param name="statusUrl"></param>
        /// <param name="maxPartySize"></param>
        /// <param name="quickPlaySetupUrl"></param>
        public void UseMasterServer(DnsEndPoint endPoint, string statusUrl, int? maxPartySize = null, string? quickPlaySetupUrl = null)
        {
            _logger.Debug($"Master server set to '{endPoint}'");
            MasterServerEndPoint = endPoint;
            MasterServerStatusUrl = statusUrl;
            MaxPartySize = maxPartySize;
            QuickPlaySetupUrl = quickPlaySetupUrl != null ? quickPlaySetupUrl : statusUrl + "/mp_override.json";
            DisableGameLift = true;
        }

        /// <summary>
        /// Uses the official servers.
        /// </summary>
        public void UseOfficialServer()
        {
            _logger.Debug($"Master server set to 'official'");
            MasterServerEndPoint = null;
            MasterServerStatusUrl = null;
            MaxPartySize = null;
            QuickPlaySetupUrl = null;
            DisableGameLift = false;
        }

        [AffinityPatch(typeof(NetworkConfigSO), nameof(NetworkConfigSO.masterServerEndPoint), AffinityMethodType.Getter)]
        private void GetMasterServerEndPoint(ref DnsEndPoint __result)
        {
            if (MasterServerEndPoint == null)
                return;

            __result = MasterServerEndPoint;
            //_logger.Debug($"Patching masterServerEndPoint with '{__result}'.");
        }

        [AffinityPatch(typeof(NetworkConfigSO), nameof(NetworkConfigSO.multiplayerStatusUrl), AffinityMethodType.Getter)]
        private void GetMasterServerStatusUrl(ref string __result)
        {
            if (MasterServerStatusUrl == null)
                return;

            __result = MasterServerStatusUrl;
            //_logger.Debug($"Patching multiplayerStatusUrl with '{__result}'.");
        }

        [AffinityPatch(typeof(NetworkConfigSO), nameof(NetworkConfigSO.maxPartySize), AffinityMethodType.Getter)]
        private void GetMaxPartySize(ref int __result)
        {
            if (MaxPartySize == null)
            {
                __result = OfficialMaxPartySize;
                return;
            }

            __result = (int)MaxPartySize;
            //_logger.Debug($"Patching master server max party size with '{__result}'.");
        }

        [AffinityPatch(typeof(NetworkConfigSO), nameof(NetworkConfigSO.quickPlaySetupUrl), AffinityMethodType.Getter)]
        private void GetQuickPlaySetupUrl(ref string __result)
        {
            //_logger.Debug("Get quickPlaySetupUrl called.");

            if (QuickPlaySetupUrl == null)
            {
                //_logger.Debug($"quickPlaySetupUrl is null.");
                return;
            }

            __result = QuickPlaySetupUrl;
            //_logger.Debug($"Patching quickPlaySetupUrl with '{__result}'.");
        }

        [AffinityPatch(typeof(NetworkConfigSO), nameof(NetworkConfigSO.discoveryPort), AffinityMethodType.Getter)]
        private void GetDiscoveryPort(ref int __result)
        {
            if (DiscoveryPort == null)
                return;

            __result = (int)DiscoveryPort;
            //_logger.Debug($"Patching network config discovery port with '{__result}'.");
        }

        [AffinityPatch(typeof(NetworkConfigSO), nameof(NetworkConfigSO.partyPort), AffinityMethodType.Getter)]
        private void GetPartyPort(ref int __result)
        {
            if (PartyPort == null)
                return;

            __result = (int)PartyPort;
            //_logger.Debug($"Patching network config party port with '{__result}'.");
        }

        [AffinityPatch(typeof(NetworkConfigSO), nameof(NetworkConfigSO.multiplayerPort), AffinityMethodType.Getter)]
        private void GetMultiplayerPort(ref int __result)
        {
            if (MultiplayerPort == null)
                return;

            __result = (int)MultiplayerPort;
            //_logger.Debug($"Patching network config multiplayer port with '{__result}'.");
        }

        [AffinityPatch(typeof(NetworkConfigSO), nameof(NetworkConfigSO.forceGameLift), AffinityMethodType.Getter)]
        private void GetForceGameLift(ref bool __result)
        {
            if (!DisableGameLift)
                return;

            __result = false;
            //_logger.Debug($"Patching network config forceGameLift with '{__result}'.");
        }
        
        [AffinityPrefix]
        [AffinityPatch(typeof(UnifiedNetworkPlayerModel), nameof(UnifiedNetworkPlayerModel.SetActiveNetworkPlayerModelType))]
        private void PrefixSetActiveNetworkPlayerModelType(ref UnifiedNetworkPlayerModel.ActiveNetworkPlayerModelType activeNetworkPlayerModelType)
        {
            if (!DisableGameLift)
                return;

            activeNetworkPlayerModelType = UnifiedNetworkPlayerModel.ActiveNetworkPlayerModelType.MasterServer;
            //_logger.Debug($"Patching activeNetworkPlayerModelType with '{activeNetworkPlayerModelType}'.");
        }

        [AffinityPrefix]
        [AffinityPatch(typeof(ClientCertificateValidator), "ValidateCertificateChainInternal")]
        private bool ValidateCertificateChain()
        {
            if (MasterServerEndPoint == null)
                return true;

            // TODO
            // It'd be best if we do certificate validation here...
            // but for now we'll just skip it.
            return false;
        }
    }
}
