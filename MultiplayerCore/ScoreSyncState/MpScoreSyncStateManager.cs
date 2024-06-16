using MultiplayerCore.Networking;
using System;
using Zenject;
using SiraUtil.Affinity;


namespace MultiplayerCore.NodePoseSyncState
{
    internal class MpScoreSyncStateManager : IInitializable, IDisposable, IAffinity
    {
        public float? DeltaUpdateFrequency { get; private set; } = null;
        public float? FullStateUpdateFrequency { get; private set; } = null;

        private readonly MpPacketSerializer _packetSerializer;
        MpScoreSyncStateManager(MpPacketSerializer packetSerializer) => _packetSerializer = packetSerializer;
        
        public void Initialize() => _packetSerializer.RegisterCallback<MpScoreSyncStatePacket>(HandleUpdateFrequencyUpdated);
        
        public void Dispose() => _packetSerializer.UnregisterCallback<MpScoreSyncStatePacket>();

        private void HandleUpdateFrequencyUpdated(MpScoreSyncStatePacket data, IConnectedPlayer player)
        {
            if (player.isConnectionOwner)
            {
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
                __result = (long)(DeltaUpdateFrequency.Value);
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
                __result = (long)(FullStateUpdateFrequency.Value);
                return false;
            }
            return true;
        }
    }
}
