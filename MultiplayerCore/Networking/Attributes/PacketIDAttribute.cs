using System;

namespace MultiplayerCore.Networking.Attributes
{
    /// <summary>
    /// An attribute for defining a packet ID for use. Without this, the class name will be used as the packet ID.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class PacketIDAttribute : Attribute
    {
        internal string ID { get; }

        /// <summary>
        /// The constructor for the PacketID
        /// </summary>
        /// <param name="id">The id to use to identify this packet.</param>
        public PacketIDAttribute(string id)
        {
            ID = id;
        }
    }
}
