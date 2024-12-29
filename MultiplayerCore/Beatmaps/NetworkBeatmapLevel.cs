using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BeatSaverSharp;
using MultiplayerCore.Beatmaps.Abstractions;
using MultiplayerCore.Beatmaps.Packets;
using MultiplayerCore.Beatmaps.Serializable;
using UnityEngine;
using static SongCore.Data.SongData;

namespace MultiplayerCore.Beatmaps
{
    /// <summary>
    /// Beatmap level data based on an MpBeatmapPacket from another player.
    /// </summary>
    class NetworkBeatmapLevel : MpBeatmap
    {
        public override string LevelHash { get; protected set; }

        public override string SongName => _packet.songName;
        public override string SongSubName => _packet.songSubName;
        public override string SongAuthorName => _packet.songAuthorName;
        public override string LevelAuthorName => _packet.levelAuthorName;
        public override float BeatsPerMinute => _packet.beatsPerMinute;
        public override float SongDuration => _packet.songDuration;

        public override Dictionary<string, Dictionary<BeatmapDifficulty, string[]>> Requirements =>
            new() { { _packet.characteristicName, _packet.requirements } };

        public override Dictionary<string, Dictionary<BeatmapDifficulty, DifficultyColors>> DifficultyColors =>
            new() { { _packet.characteristicName, _packet.mapColors } };

        public override Contributor[] Contributors => _packet.contributors;

        private readonly MpBeatmapPacket _packet;
        private readonly BeatSaver? _beatsaver;

        public NetworkBeatmapLevel(MpBeatmapPacket packet)
        {
            LevelHash = packet.levelHash;
            _packet = packet;
        }

        public NetworkBeatmapLevel(MpBeatmapPacket packet, BeatSaver beatsaver)
        {
            LevelHash = packet.levelHash;
            _packet = packet;
            _beatsaver = beatsaver;
        }

        private Task<Sprite>? _coverImageTask;

        public override Task<Sprite> TryGetCoverSpriteAsync(CancellationToken cancellationToken)
        {
            if (_coverImageTask == null)
                _coverImageTask = FetchCoverImage(cancellationToken);
            return _coverImageTask;
        }

        private async Task<Sprite> FetchCoverImage(CancellationToken cancellationToken)
        {
            if (_beatsaver == null)
                return null!;
            var beatmap = await _beatsaver.BeatmapByHash(LevelHash, cancellationToken);
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
