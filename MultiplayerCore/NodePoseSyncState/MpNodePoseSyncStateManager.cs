using MultiplayerCore.Networking;
using System;
using Zenject;
using SiraUtil.Affinity;
using SiraUtil.Logging;


namespace MultiplayerCore.NodePoseSyncState
{
    internal class MpNodePoseSyncStateManager : IInitializable, IDisposable, IAffinity
    {
        public long? DeltaUpdateFrequency { get; private set; } = null;
        public long? FullStateUpdateFrequency { get; private set; } = null;

        private readonly MpPacketSerializer _packetSerializer;
        private readonly SiraLog _logger;
        MpNodePoseSyncStateManager(MpPacketSerializer packetSerializer, SiraLog logger)
        {
            _packetSerializer = packetSerializer;
            _logger = logger;
        }
        
        public void Initialize() => _packetSerializer.RegisterCallback<MpNodePoseSyncStatePacket>(HandleUpdateFrequencyUpdated);
        
        public void Dispose() => _packetSerializer.UnregisterCallback<MpNodePoseSyncStatePacket>();

        private void HandleUpdateFrequencyUpdated(MpNodePoseSyncStatePacket data, IConnectedPlayer player)
        {
            if (player.isConnectionOwner)
            {
                DeltaUpdateFrequency = data.deltaUpdateFrequency;
                FullStateUpdateFrequency = data.fullStateUpdateFrequency;
                _logger.Debug($"Update frequency updated to DeltaUpdateFrequency {DeltaUpdateFrequency} and FullStateUpdateFrequency {FullStateUpdateFrequency}.");
            }
        }

        [AffinityPrefix]
        [AffinityPatch(typeof(NodePoseSyncStateManager), nameof(NodePoseSyncStateManager.deltaUpdateFrequencyMs), AffinityMethodType.Getter)]
        private bool GetDeltaUpdateFrequency(ref long __result)
        {
            if (DeltaUpdateFrequency.HasValue)
            {
                __result = DeltaUpdateFrequency.Value;
                return false;
            }
            return true;
        }

        [AffinityPrefix]
        [AffinityPatch(typeof(NodePoseSyncStateManager), nameof(NodePoseSyncStateManager.fullStateUpdateFrequencyMs), AffinityMethodType.Getter)]
        private bool GetFullStateUpdateFrequency(ref long __result)
        {
            if (FullStateUpdateFrequency.HasValue)
            {
                __result = FullStateUpdateFrequency.Value;
                return false;
            }
            return true;
        }
    }
}
