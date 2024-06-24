using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;
using System.Windows.Forms;
using BeatSaverSharp;
using MultiplayerCore.Beatmaps.Abstractions;
using MultiplayerCore.Beatmaps.Packets;
using SiraUtil.Zenject;

namespace MultiplayerCore.Beatmaps.Providers
{
    public class MpBeatmapLevelProvider
    {
        private readonly BeatSaver _beatsaver;
        private readonly Dictionary<string, MpBeatmap> _hashToNetworkMaps = new();
        private readonly Dictionary<string, MpBeatmap> _hashToBeatsaverMaps = new();

        internal MpBeatmapLevelProvider(
            UBinder<Plugin, BeatSaver> beatsaver)
        {
            _beatsaver = beatsaver.Value;
        }

		/// <summary>
		/// Gets an <see cref="MpBeatmap"/> for the specified level hash.
		/// </summary>
		/// <param name="levelHash">The hash of the level to get</param>
		/// <returns>An <see cref="MpBeatmap"/> with a matching level hash</returns>
		public async Task<MpBeatmap?> GetBeatmap(string levelHash)
            => GetBeatmapFromLocalBeatmaps(levelHash)
            ?? await GetBeatmapFromBeatSaver(levelHash);

		/// <summary>
		/// Gets an <see cref="MpBeatmap"/> for the specified level hash from local, already downloaded beatmaps.
		/// </summary>
		/// <param name="levelHash">The hash of the level to get</param>
		/// <returns>An <see cref="MpBeatmap"/> with a matching level hash, or null if none was found.</returns>
		public MpBeatmap? GetBeatmapFromLocalBeatmaps(string levelHash)
        {
            var localBeatmapLevel = SongCore.Loader.GetLevelByHash(levelHash);
            if (localBeatmapLevel == null)
                return null;
            
            return new LocalBeatmapLevel(levelHash, localBeatmapLevel);
        }

		/// <summary>
		/// Gets an <see cref="MpBeatmap"/> for the specified level hash from BeatSaver.
		/// </summary>
		/// <param name="levelHash">The hash of the level to get</param>
		/// <returns>An <see cref="MpBeatmap"/> with a matching level hash, or null if none was found.</returns>
		public async Task<MpBeatmap?> GetBeatmapFromBeatSaver(string levelHash)
        {
            if (_hashToBeatsaverMaps.TryGetValue(levelHash, out var map)) return map;
            var beatmap = await _beatsaver.BeatmapByHash(levelHash);
            if (beatmap != null)
            {
                map = new BeatSaverBeatmapLevel(levelHash, beatmap);
                _hashToBeatsaverMaps.Add(levelHash, map);
                return map;
            }

            return null;
        }

        public BeatSaverPreviewMediaData MakeBeatSaverPreviewMediaData(string levelHash) => new BeatSaverPreviewMediaData(_beatsaver, levelHash);

		/// <summary>
		/// Gets an <see cref="MpBeatmap"/> from the information in the provided packet.
		/// </summary>
		/// <param name="packet">The packet to get preview data from</param>
		/// <returns>An <see cref="MpBeatmap"/> with a cover from BeatSaver.</returns>
		public MpBeatmap GetBeatmapFromPacket(MpBeatmapPacket packet)
        {
            if (_hashToNetworkMaps.TryGetValue(packet.levelHash, out var map)) return map;
            map = new NetworkBeatmapLevel(packet);
            _hashToNetworkMaps.Add(packet.levelHash, map);
            return map;
        }
    }
}
