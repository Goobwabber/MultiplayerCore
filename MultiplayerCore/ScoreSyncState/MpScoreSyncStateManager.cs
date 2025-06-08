using MultiplayerCore.Networking;
using System;
using Zenject;
using SiraUtil.Affinity;
using SiraUtil.Logging;


namespace MultiplayerCore.NodePoseSyncState
{
    internal class MpScoreSyncStateManager : IInitializable, IDisposable, IAffinity
    {
        public long? DeltaUpdateFrequency { get; private set; } 
        public long? FullStateUpdateFrequency { get; private set; }
        public bool ShouldForceUpdate { get; private set; }

		private readonly MpPacketSerializer _packetSerializer;
		private readonly SiraLog _logger;

		MpScoreSyncStateManager(MpPacketSerializer packetSerializer, SiraLog logger)
		{
			_packetSerializer = packetSerializer;
			_logger = logger;
		}

		public void Initialize() => _packetSerializer.RegisterCallback<MpScoreSyncStatePacket>(HandleUpdateFrequencyUpdated);
        
        public void Dispose() => _packetSerializer.UnregisterCallback<MpScoreSyncStatePacket>();

        private void HandleUpdateFrequencyUpdated(MpScoreSyncStatePacket data, IConnectedPlayer player)
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
        [AffinityPatch(typeof(ScoreSyncStateManager), "deltaUpdateFrequencyMs", AffinityMethodType.Getter)]
        private bool GetDeltaUpdateFrequencyMs(ref long __result)
        {
            if (DeltaUpdateFrequency.HasValue)
            {
                __result = DeltaUpdateFrequency.Value;
                return false;
            }
            return true;
        }

        [AffinityPrefix]
        [AffinityPatch(typeof(ScoreSyncStateManager), "fullStateUpdateFrequencyMs", AffinityMethodType.Getter)]
        private bool GetFullStateUpdateFrequencyMs(ref long __result)
        {
            if (FullStateUpdateFrequency.HasValue)
            {
                __result = FullStateUpdateFrequency.Value;
                return false;
            }
            return true;
        }

        [AffinityPrefix]
        [AffinityPatch(typeof(MultiplayerSyncStateManager<StandardScoreSyncState, StandardScoreSyncState.Score, int, StandardScoreSyncStateNetSerializable, StandardScoreSyncStateDeltaNetSerializable>), nameof(ScoreSyncStateManager.TryCreateLocalState))]
        private void TryCreateLocalState(MultiplayerSyncStateManager<StandardScoreSyncState, StandardScoreSyncState.Score, int, StandardScoreSyncStateNetSerializable, StandardScoreSyncStateDeltaNetSerializable> __instance)
        {
	        if (ShouldForceUpdate)
	        {
		        _logger.Debug("Forcing new state buffer update");
		        __instance._localState = null;
		        ShouldForceUpdate = false;
	        }
        }

        [AffinityPrefix]
        [AffinityPatch(typeof(MultiplayerSyncStateManager<StandardScoreSyncState, StandardScoreSyncState.Score, int, StandardScoreSyncStateNetSerializable, StandardScoreSyncStateDeltaNetSerializable>), nameof(ScoreSyncStateManager.HandlePlayerConnected))]
        private void HandlePlayerConnected(MultiplayerSyncStateManager<StandardScoreSyncState, StandardScoreSyncState.Score, int, StandardScoreSyncStateNetSerializable, StandardScoreSyncStateDeltaNetSerializable> __instance)
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
