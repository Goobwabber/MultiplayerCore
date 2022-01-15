using LiteNetLib.Utils;
using MultiplayerCore.Networking.Abstractions;
using System.Collections.Generic;

namespace MultiplayerCore.Players
{
    public class MpPlayerData : MpPacket
    {
        /// <summary>
        /// Platform User ID from <see cref="UserInfo.platformUserId"/>
        /// </summary>
        public string PlatformId { get; set; } = string.Empty;

        /// <summary>
        /// Platform from <see cref="UserInfo.platformUserId">
        /// </summary>
        public Platform Platform { get; set; }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(PlatformId);
            writer.Put((int)Platform);
        }

        public override void Deserialize(NetDataReader reader)
        {
            PlatformId = reader.GetString();
            Platform = (Platform)reader.GetInt();
        }
    }

    public enum Platform
    {
        Unknown = 0,
        Steam = 1,
        OculusPC = 2,
        OculusQuest = 3,
        PS4 = 4
    }
}
