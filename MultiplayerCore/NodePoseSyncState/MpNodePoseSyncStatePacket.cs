using MultiplayerCore.Networking.Abstractions;
using LiteNetLib.Utils;

namespace MultiplayerCore.NodePoseSyncState
{
    internal class MpNodePoseSyncStatePacket : MpPacket
    {
        public float deltaUpdateFrequency = 0.01f;
        public float fullStateUpdateFrequency = 0.1f;
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(deltaUpdateFrequency);
            writer.Put(fullStateUpdateFrequency);
        }

        public override void Deserialize(NetDataReader reader)
        {
            deltaUpdateFrequency = reader.GetFloat();
            fullStateUpdateFrequency = reader.GetFloat();
        }
    }
}
