using LiteNetLib.Utils;

namespace MultiplayerCore.Networking.Abstractions
{
    public abstract class MpPacket : INetSerializable
    {
        /// <summary>
        /// Serializes the packet and puts data into a <see cref="NetDataWriter"/>.
        /// </summary>
        /// <param name="writer">Writer to put data into</param>
        public abstract void Serialize(NetDataWriter writer);

        /// <summary>
        /// Deserializes packet data from a <see cref="NetDataReader"/>.
        /// </summary>
        /// <param name="reader">Reader to get data from</param>
        public abstract void Deserialize(NetDataReader reader);
    }
}
