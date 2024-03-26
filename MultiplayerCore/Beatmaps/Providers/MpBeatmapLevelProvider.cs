using System.Threading.Tasks;
using BeatSaverSharp;
using MultiplayerCore.Beatmaps.Abstractions;
using MultiplayerCore.Beatmaps.Packets;
using SiraUtil.Zenject;

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
        public async Task<MpBeatmap?> GetBeatmap(string levelHash)
            => GetBeatmapFromLocalBeatmaps(levelHash)
            ?? await GetBeatmapFromBeatSaver(levelHash);

        /// <summary>
        /// Gets an <see cref="IPreviewBeatmapLevel"/> for the specified level hash from local, already downloaded beatmaps.
        /// </summary>
        /// <param name="levelHash">The hash of the level to get</param>
        /// <returns>An <see cref="IPreviewBeatmapLevel"/> with a matching level hash, or null if none was found.</returns>
        public MpBeatmap? GetBeatmapFromLocalBeatmaps(string levelHash)
        {
            var localBeatmapLevel = SongCore.Loader.GetLevelByHash(levelHash);
            if (localBeatmapLevel == null)
                return null;
            
            return new LocalBeatmapLevel(levelHash, localBeatmapLevel);
        }

        /// <summary>
        /// Gets an <see cref="IPreviewBeatmapLevel"/> for the specified level hash from BeatSaver.
        /// </summary>
        /// <param name="levelHash">The hash of the level to get</param>
        /// <returns>An <see cref="IPreviewBeatmapLevel"/> with a matching level hash, or null if none was found.</returns>
        public async Task<MpBeatmap?> GetBeatmapFromBeatSaver(string levelHash)
        {
            var beatmap = await _beatsaver.BeatmapByHash(levelHash);
            if (beatmap == null)
                return null;
            
            return new BeatSaverBeatmapLevel(levelHash, beatmap);
        }

        /// <summary>
        /// Gets an <see cref="IPreviewBeatmapLevel"/> from the information in the provided packet.
        /// </summary>
        /// <param name="packet">The packet to get preview data from</param>
        /// <returns>An <see cref="IPreviewBeatmapLevel"/> with a cover from BeatSaver.</returns>
        public MpBeatmap GetBeatmapFromPacket(MpBeatmapPacket packet)
            => new NetworkBeatmapLevel(packet, _beatsaver);
    }
}
