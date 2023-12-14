using MultiplayerCore.Networking.Abstractions;
using LiteNetLib.Utils;

namespace MultiplayerCore.NodePoseSyncState
{
    internal class MpNodePoseSyncStatePacket : MpPacket
    {
        public long deltaUpdateFrequency = 10L;
        public long fullStateUpdateFrequency = 100L;
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(deltaUpdateFrequency);
            writer.Put(fullStateUpdateFrequency);
        }

        public override void Deserialize(NetDataReader reader)
        {
            deltaUpdateFrequency = reader.GetLong();
            fullStateUpdateFrequency = reader.GetLong();
        }
    }
}
