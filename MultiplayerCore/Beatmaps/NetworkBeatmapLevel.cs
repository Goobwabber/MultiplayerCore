using MultiplayerCore.Beatmaps.Abstractions;
using MultiplayerCore.Beatmaps.Packets;

namespace MultiplayerCore.Beatmaps
{
    /// <summary>
    /// An <see cref="IPreviewBeatmapLevel"/> created from data from a network player.
    /// </summary>
    class NetworkBeatmapLevel : MpBeatmapLevel
    {
        public override string levelHash { get; protected set; }

        public override string songName => _packet.songName;
        public override string songSubName => _packet.songSubName;
        public override string songAuthorName => _packet.songAuthorName;
        public override string levelAuthorName => _packet.levelAuthorName;
        public override float beatsPerMinute => _packet.beatsPerMinute;
        public override float songDuration => _packet.songDuration;

        private readonly MpBeatmapPacket _packet;

        public NetworkBeatmapLevel(MpBeatmapPacket packet)
        {
            levelHash = packet.levelHash;
            _packet = packet;
        }
    }
}
