using SiraUtil.Affinity;
using SiraUtil.Logging;

// ReSharper disable RedundantAssignment
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable InconsistentNaming

namespace MultiplayerCore.Patchers
{
    public class NetworkConfigPatcher : IAffinity
    {
        public const int OfficialMaxPartySize = 5;

        public string? GraphUrl { get; set; }
        public string? MasterServerStatusUrl { get; set; }
        public string? QuickPlaySetupUrl { get; set; }
        public int? MaxPartySize { get; set; }
        public int? DiscoveryPort { get; set; }
        public int? PartyPort { get; set; }
        public int? MultiplayerPort { get; set; }

        public bool IsOverridingApi => GraphUrl != null;

        private readonly SiraLog _logger;

        internal NetworkConfigPatcher(
            SiraLog logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Override official servers with a custom API server.
        /// </summary>
        public void UseCustomApiServer(string graphUrl, string statusUrl, int? maxPartySize = null,
            string? quickPlaySetupUrl = null)
        {
            _logger.Debug($"Overriding multiplayer API server (graphUrl={graphUrl}, statusUrl={statusUrl}, " +
                          $"maxPartySize={maxPartySize}, quickPlaySetupUrl={quickPlaySetupUrl})");

            GraphUrl = graphUrl;
            MasterServerStatusUrl = statusUrl;
            MaxPartySize = maxPartySize;
            QuickPlaySetupUrl = quickPlaySetupUrl ?? statusUrl + "/mp_override.json";
        }

        /// <summary>
        /// Use the official API server and disable any override.
        /// </summary>
        public void UseOfficialServer()
        {
            if (!IsOverridingApi)
                return;

            _logger.Debug($"Removed multiplayer API server override");

            GraphUrl = null;
            MasterServerStatusUrl = null;
            MaxPartySize = null;
            QuickPlaySetupUrl = null;
        }

        [AffinityPatch(typeof(NetworkConfigSO), nameof(NetworkConfigSO.graphUrl), AffinityMethodType.Getter)]
        private void GetGraphUrl(ref string __result)
        {
            if (!IsOverridingApi)
                return;

            __result = GraphUrl!;
        }

        [AffinityPatch(typeof(NetworkConfigSO), nameof(NetworkConfigSO.multiplayerStatusUrl),
            AffinityMethodType.Getter)]
        private void GetMasterServerStatusUrl(ref string __result)
        {
            if (MasterServerStatusUrl == null)
                return;

            __result = MasterServerStatusUrl!;
        }

        [AffinityPatch(typeof(NetworkConfigSO), nameof(NetworkConfigSO.maxPartySize), AffinityMethodType.Getter)]
        private void GetMaxPartySize(ref int __result)
        {
            if (MaxPartySize == null)
            {
                __result = OfficialMaxPartySize;
                return;
            }

            __result = MaxPartySize.Value;
        }

        [AffinityPatch(typeof(NetworkConfigSO), nameof(NetworkConfigSO.quickPlaySetupUrl), AffinityMethodType.Getter)]
        private void GetQuickPlaySetupUrl(ref string __result)
        {
            if (QuickPlaySetupUrl == null)
                return;

            __result = QuickPlaySetupUrl;
        }

        [AffinityPatch(typeof(NetworkConfigSO), nameof(NetworkConfigSO.discoveryPort), AffinityMethodType.Getter)]
        private void GetDiscoveryPort(ref int __result)
        {
            if (DiscoveryPort == null)
                return;

            __result = DiscoveryPort.Value;
        }

        [AffinityPatch(typeof(NetworkConfigSO), nameof(NetworkConfigSO.partyPort), AffinityMethodType.Getter)]
        private void GetPartyPort(ref int __result)
        {
            if (PartyPort == null)
                return;

            __result = PartyPort.Value;
        }

        [AffinityPatch(typeof(NetworkConfigSO), nameof(NetworkConfigSO.multiplayerPort), AffinityMethodType.Getter)]
        private void GetMultiplayerPort(ref int __result)
        {
            if (MultiplayerPort == null)
                return;

            __result = MultiplayerPort.Value;
        }

        [AffinityPatch(typeof(NetworkConfigSO), nameof(NetworkConfigSO.forceGameLift), AffinityMethodType.Getter)]
        private void GetForceGameLift(ref bool __result)
        {
            // If we're overriding the API, the game should always use GameLift connection manager flow
            __result = !IsOverridingApi;
        }

        [AffinityPrefix]
        [AffinityPatch(typeof(UnifiedNetworkPlayerModel),
            nameof(UnifiedNetworkPlayerModel.SetActiveNetworkPlayerModelType))]
        private void PrefixSetActiveNetworkPlayerModelType(
            ref UnifiedNetworkPlayerModel.ActiveNetworkPlayerModelType activeNetworkPlayerModelType)
        {
            if (!IsOverridingApi)
                return;

            // If we're overriding the API, the game should always use GameLift connection manager flow
            activeNetworkPlayerModelType = UnifiedNetworkPlayerModel.ActiveNetworkPlayerModelType.GameLift;
        }

        [AffinityPrefix]
        [AffinityPatch(typeof(ClientCertificateValidator), "ValidateCertificateChainInternal")]
        private bool ValidateCertificateChain()
        {
            return !IsOverridingApi;

            // TODO
            // It'd be best if we do certificate validation here...
            // but for now we'll just skip it.
        }
    }
}