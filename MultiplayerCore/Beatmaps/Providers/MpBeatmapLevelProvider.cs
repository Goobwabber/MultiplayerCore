using BeatSaverSharp;
using BeatSaverSharp.Models;
using MultiplayerCore.Beatmaps.Packets;
using SiraUtil.Logging;
using SiraUtil.Zenject;
using System.Threading.Tasks;

namespace MultiplayerCore.Beatmaps.Providers
{
    public class MpBeatmapLevelProvider
    {
        private readonly BeatSaver _beatsaver;

        internal MpBeatmapLevelProvider(
            UBinder<Plugin, BeatSaver> beatsaver)
        {
            _beatsaver = beatsaver.Value;
        }

        /// <summary>
        /// Gets an <see cref="IPreviewBeatmapLevel"/> for the specified level hash.
        /// </summary>
        /// <param name="levelHash">The hash of the level to get</param>
        /// <returns>An <see cref="IPreviewBeatmapLevel"/> with a matching level hash</returns>
        public async Task<IPreviewBeatmapLevel?> GetBeatmap(string levelHash)
            => GetBeatmapFromLocalBeatmaps(levelHash)
            ?? await GetBeatmapFromBeatSaver(levelHash);

        /// <summary>
        /// Gets an <see cref="IPreviewBeatmapLevel"/> for the specified level hash from local, already downloaded beatmaps.
        /// </summary>
        /// <param name="levelHash">The hash of the level to get</param>
        /// <returns>An <see cref="IPreviewBeatmapLevel"/> with a matching level hash, or null if none was found.</returns>
        public IPreviewBeatmapLevel? GetBeatmapFromLocalBeatmaps(string levelHash)
        {
            IPreviewBeatmapLevel? preview = SongCore.Loader.GetLevelByHash(levelHash);
            if (preview == null)
                return null;
            return new LocalBeatmapLevel(levelHash, preview);
        }

        /// <summary>
        /// Gets an <see cref="IPreviewBeatmapLevel"/> for the specified level hash from BeatSaver.
        /// </summary>
        /// <param name="levelHash">The hash of the level to get</param>
        /// <returns>An <see cref="IPreviewBeatmapLevel"/> with a matching level hash, or null if none was found.</returns>
        public async Task<IPreviewBeatmapLevel?> GetBeatmapFromBeatSaver(string levelHash)
        {
            Beatmap? beatmap = await _beatsaver.BeatmapByHash(levelHash);
            if (beatmap == null)
                return null;
            return new BeatSaverBeatmapLevel(levelHash, beatmap);
        }

        /// <summary>
        /// Gets an <see cref="IPreviewBeatmapLevel"/> from the information in the provided packet.
        /// </summary>
        /// <param name="packet">The packet to get preview data from</param>
        /// <returns>An <see cref="IPreviewBeatmapLevel"/> with a cover from BeatSaver.</returns>
        public IPreviewBeatmapLevel GetBeatmapFromPacket(MpBeatmapPacket packet)
            => new NetworkBeatmapLevel(packet, _beatsaver);
    }
}
