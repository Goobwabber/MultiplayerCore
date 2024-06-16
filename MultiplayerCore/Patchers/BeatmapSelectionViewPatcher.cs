using MultiplayerCore.Beatmaps;
using MultiplayerCore.Beatmaps.Abstractions;
using MultiplayerCore.Beatmaps.Packets;
using MultiplayerCore.Beatmaps.Providers;
using MultiplayerCore.Objects;
using System;
using System.Collections.Generic;
using SiraUtil.Affinity;

namespace MultiplayerCore.Patchers
{
    internal class BeatmapSelectionViewPatcher : IAffinity
    {
        private MpPlayersDataModel _mpPlayersDataModel;
        private MpBeatmapLevelProvider _mpBeatmapLevelProvider;
        private BeatmapLevelsModel _beatmapLevelsModel;


        BeatmapSelectionViewPatcher(ILobbyPlayersDataModel playersDataModel, MpBeatmapLevelProvider mpBeatmapLevelProvider, BeatmapLevelsModel beatmapLevelsModel)
        {
            _mpPlayersDataModel = playersDataModel as MpPlayersDataModel;
            _mpBeatmapLevelProvider = mpBeatmapLevelProvider;
            _beatmapLevelsModel = beatmapLevelsModel;
        }

        [AffinityPrefix]
        [AffinityPatch(typeof(EditableBeatmapSelectionView), nameof(EditableBeatmapSelectionView.SetBeatmap))]
        public bool EditableBeatmapSelectionView_SetBeatmap(ref EditableBeatmapSelectionView __instance, in BeatmapKey beatmapKey)
        {
            if (_mpPlayersDataModel == null) return false;
            if (!beatmapKey.IsValid()) return true;
            if (_beatmapLevelsModel.GetBeatmapLevel(beatmapKey.levelId) != null) return true;

            var levelHash = Utilities.HashForLevelID(beatmapKey.levelId);
            if (String.IsNullOrWhiteSpace(levelHash)) return true;

            var packet = _mpPlayersDataModel.FindLevelPacket(levelHash!);
            if (packet == null) return true;

            __instance._clearButton.gameObject.SetActive(__instance.showClearButton);
            __instance._noLevelText.enabled = false;
            __instance._levelBar.hide = false;

            var level = _mpBeatmapLevelProvider.GetBeatmapFromPacket(packet).MakeBeatmapLevel(beatmapKey, _mpBeatmapLevelProvider.MakeBeatSaverPreviewMediaData(packet.levelHash));
			__instance._levelBar.SetupData(level, beatmapKey.difficulty, beatmapKey.beatmapCharacteristic);
			return false;
        }

        [AffinityPrefix]
        [AffinityPatch(typeof(BeatmapSelectionView), nameof(BeatmapSelectionView.SetBeatmap))]
        public bool BeatmapSelectionView_SetBeatmap(ref BeatmapSelectionView __instance, in BeatmapKey beatmapKey)
        {
            if (_mpPlayersDataModel == null) return false;
            if (!beatmapKey.IsValid()) return true;
            if (_beatmapLevelsModel.GetBeatmapLevel(beatmapKey.levelId) != null) return true;

            var levelHash = Utilities.HashForLevelID(beatmapKey.levelId);
            if (String.IsNullOrWhiteSpace(levelHash)) return true;

            var packet = _mpPlayersDataModel.FindLevelPacket(levelHash!);
            if (packet == null) return true;

            __instance._noLevelText.enabled = false;
            __instance._levelBar.hide = false;

            var level = _mpBeatmapLevelProvider.GetBeatmapFromPacket(packet).MakeBeatmapLevel(beatmapKey, _mpBeatmapLevelProvider.MakeBeatSaverPreviewMediaData(packet.levelHash));
            __instance._levelBar.SetupData(level, beatmapKey.difficulty, beatmapKey.beatmapCharacteristic);
            return false;
        }
    }

    static internal class PacketExt
    {
        public static BeatmapLevel MakeBeatmapLevel(this MpBeatmap mpBeatmap, in BeatmapKey key, IPreviewMediaData previewMediaData) 
        {
            var dict = new Dictionary<(BeatmapCharacteristicSO, BeatmapDifficulty), BeatmapBasicData>();
            dict[(key.beatmapCharacteristic, key.difficulty)] = new BeatmapBasicData(
                0, 
                0, 
                EnvironmentName.Empty, 
                null, 
                0, 
                0, 
                0, 
                new[] { mpBeatmap.LevelAuthorName }, 
                Array.Empty<string>()
            );

            return new BeatmapLevel(
                0,
                false,
                mpBeatmap.LevelID,
                mpBeatmap.SongName,
                mpBeatmap.SongAuthorName,
                mpBeatmap.SongSubName,
                new[] { mpBeatmap.LevelAuthorName },
                Array.Empty<string>(),
                mpBeatmap.BeatsPerMinute,
                -6.0f,
                0,
                0,
                0,
                mpBeatmap.SongDuration,
                PlayerSensitivityFlag.Safe,
                previewMediaData,
                dict
            );
        }
    }
}
