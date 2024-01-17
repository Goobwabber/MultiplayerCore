using MultiplayerCore.Networking;
using System;
using Zenject;
using SiraUtil.Affinity;


namespace MultiplayerCore.NodePoseSyncState
{
    internal class MpNodePoseSyncStateManager : IInitializable, IDisposable, IAffinity
    {
        public float? DeltaUpdateFrequency { get; private set; } = null;
        public float? FullStateUpdateFrequency { get; private set; } = null;

        private readonly MpPacketSerializer _packetSerializer;
        MpNodePoseSyncStateManager(MpPacketSerializer packetSerializer) => _packetSerializer = packetSerializer;
        
        public void Initialize() => _packetSerializer.RegisterCallback<MpNodePoseSyncStatePacket>(HandleUpdateFrequencyUpdated);
        
        public void Dispose() => _packetSerializer.UnregisterCallback<MpNodePoseSyncStatePacket>();

        private void HandleUpdateFrequencyUpdated(MpNodePoseSyncStatePacket data, IConnectedPlayer player)
        {
            if (player.isConnectionOwner)
            {
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
                __result = (long)(DeltaUpdateFrequency.Value * 1000);
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
                __result = (long)(FullStateUpdateFrequency.Value * 1000);
                return false;
            }
            return true;
        }
    }
}
