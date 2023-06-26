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
        [AffinityPatch(typeof(NodePoseSyncStateManager), "deltaUpdateFrequency", AffinityMethodType.Getter)]
        private bool GetDeltaUpdateFrequency(ref float __result)
        {
            if (DeltaUpdateFrequency.HasValue)
            {
                __result = DeltaUpdateFrequency.Value;
                return false;
            }
            return true;
        }

        [AffinityPrefix]
        [AffinityPatch(typeof(NodePoseSyncStateManager), "fullStateUpdateFrequency", AffinityMethodType.Getter)]
        private bool GetFullStateUpdateFrequency(ref float __result)
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
