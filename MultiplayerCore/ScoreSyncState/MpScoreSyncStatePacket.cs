using MultiplayerCore.Networking.Abstractions;
using LiteNetLib.Utils;

namespace MultiplayerCore.NodePoseSyncState
{
    internal class MpScoreSyncStatePacket : MpPacket
    {
        public long deltaUpdateFrequency = 100L;
        public long fullStateUpdateFrequency = 500L;
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(deltaUpdateFrequency);
            writer.Put(fullStateUpdateFrequency);
        }

        public override void Deserialize(NetDataReader reader)
        {
            deltaUpdateFrequency = reader.GetVarLong();
            fullStateUpdateFrequency = reader.GetVarLong();
        }
    }
}
