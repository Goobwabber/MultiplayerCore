using Hive.Versioning;
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

        /// <summary>
        /// Version
        /// </summary>
        public Version GameVersion { get; set; }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(PlatformId);
            writer.Put((int)Platform);
            writer.Put(GameVersion.ToString());
        }

        public override void Deserialize(NetDataReader reader)
        {
            PlatformId = reader.GetString();
            Platform = (Platform)reader.GetInt();
            GameVersion = Version.Parse(reader.GetString());
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
