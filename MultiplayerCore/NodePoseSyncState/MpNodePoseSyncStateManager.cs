using MultiplayerCore.Networking;
using System;
using Zenject;
using SiraUtil.Affinity;
using SiraUtil.Logging;


namespace MultiplayerCore.NodePoseSyncState
{
    internal class MpNodePoseSyncStateManager : IInitializable, IDisposable, IAffinity
    {
		public long? DeltaUpdateFrequency { get; private set; }
        public long? FullStateUpdateFrequency { get; private set; }

        public bool ShouldForceUpdate { get; private set; }

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
                _logger.Debug("Updating node pose sync frequency to following values: " +
                              $"delta: {data.deltaUpdateFrequency}ms, full: {data.fullStateUpdateFrequency}ms");
                ShouldForceUpdate = DeltaUpdateFrequency != data.deltaUpdateFrequency ||
                                    FullStateUpdateFrequency != data.fullStateUpdateFrequency;
                DeltaUpdateFrequency = data.deltaUpdateFrequency;
                FullStateUpdateFrequency = data.fullStateUpdateFrequency;
            }
        }

        [AffinityPrefix]
        [AffinityPatch(typeof(NodePoseSyncStateManager), "deltaUpdateFrequencyMs", AffinityMethodType.Getter)]
        private bool GetDeltaUpdateFrequencyMs(ref long __result)
        {
            if (DeltaUpdateFrequency.HasValue)
            {
                _logger.Debug($"Returning delta update frequency: {DeltaUpdateFrequency.Value}ms");
                __result = DeltaUpdateFrequency.Value;
                return false;
            }
            return true;
        }

        [AffinityPrefix]
        [AffinityPatch(typeof(NodePoseSyncStateManager), "fullStateUpdateFrequencyMs", AffinityMethodType.Getter)]
        private bool GetFullStateUpdateFrequencyMs(ref long __result)
        {
            if (FullStateUpdateFrequency.HasValue)
            {
	            _logger.Debug($"Returning full state update frequency: {FullStateUpdateFrequency.Value}ms");
                __result = FullStateUpdateFrequency.Value;
                return false;
            }
            return true;
        }

        [AffinityPrefix]
        [AffinityPatch(typeof(MultiplayerSyncStateManager<global::NodePoseSyncState, global::NodePoseSyncState.NodePose, PoseSerializable, NodePoseSyncStateNetSerializable, NodePoseSyncStateDeltaNetSerializable>), nameof(NodePoseSyncStateManager.TryCreateLocalState))]
        private void TryCreateLocalState(MultiplayerSyncStateManager<global::NodePoseSyncState, global::NodePoseSyncState.NodePose, PoseSerializable, NodePoseSyncStateNetSerializable, NodePoseSyncStateDeltaNetSerializable> __instance)
        {
	        if (ShouldForceUpdate)
	        {
		        _logger.Debug("Forcing new state buffer update");
				__instance._localState = null;
                ShouldForceUpdate = false;
	        }
        }

        [AffinityPrefix]
        [AffinityPatch(typeof(MultiplayerSyncStateManager<global::NodePoseSyncState, global::NodePoseSyncState.NodePose, PoseSerializable, NodePoseSyncStateNetSerializable, NodePoseSyncStateDeltaNetSerializable>), nameof(NodePoseSyncStateManager.HandlePlayerConnected))]
        private void HandlePlayerConnected(MultiplayerSyncStateManager<global::NodePoseSyncState, global::NodePoseSyncState.NodePose, PoseSerializable, NodePoseSyncStateNetSerializable, NodePoseSyncStateDeltaNetSerializable> __instance)
        {
	        if (ShouldForceUpdate)
	        {
                _logger.Debug("Forcing new state buffer update");
		        __instance._localState = null;
                ShouldForceUpdate = false;
	        }
		}
	}
}
