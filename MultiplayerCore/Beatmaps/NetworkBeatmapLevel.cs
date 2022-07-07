using BeatSaverSharp;
using MultiplayerCore.Beatmaps.Abstractions;
using MultiplayerCore.Beatmaps.Packets;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using static SongCore.Data.ExtraSongData;

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

        public override Dictionary<BeatmapDifficulty, string[]> requirements => _packet.requirements;
        public override Contributor[] contributors => _packet.contributors;

        private readonly MpBeatmapPacket _packet;
        private readonly BeatSaver? _beatsaver;

        public NetworkBeatmapLevel(MpBeatmapPacket packet)
        {
            levelHash = packet.levelHash;
            _packet = packet;
        }

        public NetworkBeatmapLevel(MpBeatmapPacket packet, BeatSaver beatsaver)
        {
            levelHash = packet.levelHash;
            _packet = packet;
            _beatsaver = beatsaver;
        }

        private Task<Sprite>? _coverImageTask;

        public override Task<Sprite> GetCoverImageAsync(CancellationToken cancellationToken)
        {
            if (_coverImageTask == null)
                _coverImageTask = FetchCoverImage(cancellationToken);
            return _coverImageTask;
        }

        private async Task<Sprite> FetchCoverImage(CancellationToken cancellationToken)
        {
            if (_beatsaver == null)
                return null!;
            var beatmap = await _beatsaver.BeatmapByHash(levelHash, cancellationToken);
            if (beatmap == null)
                return null!;
            byte[]? coverBytes = await beatmap.LatestVersion.DownloadCoverImage(cancellationToken);
            if (coverBytes == null || coverBytes.Length == 0)
                return null!;
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(coverBytes);
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0, 0), 100.0f);
        }
    }
}
