﻿using SiraUtil.Affinity;
using SiraUtil.Logging;

namespace MultiplayerCore.Patchers
{
    public class NetworkConfigPatcher : IAffinity
    {
        public const int OfficialMaxPartySize = 5;

        public DnsEndPoint? MasterServerEndPoint { get; set; }
        public string? MasterServerStatusUrl { get; set; }
        public int? MaxPartySize { get; set; }
        public int? DiscoveryPort { get; set; }
        public int? PartyPort { get; set; }
        public int? MultiplayerPort { get; set; }

        private readonly SiraLog _logger;
        private readonly INetworkConfig _networkConfig;

        internal NetworkConfigPatcher(
            INetworkConfig networkConfig,
            SiraLog logger)
        {
            _networkConfig = networkConfig;
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
        }

        [AffinityPatch(typeof(NetworkConfigSO), nameof(NetworkConfigSO.masterServerEndPoint), AffinityMethodType.Getter)]
        private void GetMasterServerEndPoint(ref DnsEndPoint __result)
        {
            if (MasterServerEndPoint == null)
                return;

            __result = MasterServerEndPoint;
        }

        [AffinityPatch(typeof(NetworkConfigSO), nameof(NetworkConfigSO.multiplayerStatusUrl), AffinityMethodType.Getter)]
        private void GetMasterServerStatusUrl(ref string __result)
        {
            if (MasterServerStatusUrl == null)
                return;

            __result = MasterServerStatusUrl;
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
            _logger.Debug($"Patching master server max party size with '{__result}'.");
        }

        [AffinityPatch(typeof(NetworkConfigSO), nameof(NetworkConfigSO.discoveryPort), AffinityMethodType.Getter)]
        private void GetDiscoveryPort(ref int __result)
        {
            if (DiscoveryPort == null)
                return;

            __result = (int)DiscoveryPort;
            _logger.Debug($"Patching network config discovery port with '{__result}'.");
        }

        [AffinityPatch(typeof(NetworkConfigSO), nameof(NetworkConfigSO.partyPort), AffinityMethodType.Getter)]
        private void GetPartyPort(ref int __result)
        {
            if (PartyPort == null)
                return;

            __result = (int)PartyPort;
            _logger.Debug($"Patching network config party port with '{__result}'.");
        }

        [AffinityPatch(typeof(NetworkConfigSO), nameof(NetworkConfigSO.multiplayerPort), AffinityMethodType.Getter)]
        private void GetMultiplayerPort(ref int __result)
        {
            if (MultiplayerPort == null)
                return;

            __result = (int)MultiplayerPort;
            _logger.Debug($"Patching network config multiplayer port with '{__result}'.");
        }

        [AffinityPrefix]
        [AffinityPatch(typeof(ClientCertificateValidator), "ValidateCertificateChainInternal")]
        private bool ValidateCertificateChain()
        {
            if (MasterServerEndPoint == null && !(_networkConfig is CustomNetworkConfig))
                return true;

            // TODO
            // It'd be best if we do certificate validation here...
            // but for now we'll just skip it.
            return false;
        }
    }
}
