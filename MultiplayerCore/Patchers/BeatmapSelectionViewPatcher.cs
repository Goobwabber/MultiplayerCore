using HarmonyLib;
using MultiplayerCore.Beatmaps.Abstractions;
using MultiplayerCore.Beatmaps.Providers;
using MultiplayerCore.Objects;
using SiraUtil.Affinity;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace MultiplayerCore.Patchers
{
	internal class BeatmapSelectionViewPatcher : IAffinity
    {
        private MpPlayersDataModel _mpPlayersDataModel;
        private MpBeatmapLevelProvider _mpBeatmapLevelProvider;
        private BeatmapLevelsModel _beatmapLevelsModel;

        private static MethodInfo _lbarInfo;
        private static bool _newlbarInfo;


        BeatmapSelectionViewPatcher(ILobbyPlayersDataModel playersDataModel, MpBeatmapLevelProvider mpBeatmapLevelProvider, BeatmapLevelsModel beatmapLevelsModel)
        {
            _mpPlayersDataModel = playersDataModel as MpPlayersDataModel;
            _mpBeatmapLevelProvider = mpBeatmapLevelProvider;
            _beatmapLevelsModel = beatmapLevelsModel;

            _lbarInfo = AccessTools.Method(typeof(LevelBar), "SetupData",
	            new Type[] { typeof(BeatmapLevel), typeof(BeatmapDifficulty), typeof(BeatmapCharacteristicSO) });
            if (_lbarInfo != null) _newlbarInfo = true;
			else _lbarInfo = AccessTools.Method(typeof(LevelBar), "Setup", new Type[] { typeof(BeatmapLevel), typeof(BeatmapCharacteristicSO), typeof(BeatmapDifficulty) });
			if (_lbarInfo == null)
			{
				Plugin.Logger.Critical("Can't find a fitting LevelBar Method, is your game version supported?");
			}
		}

		[AffinityPrefix]
        [AffinityPatch(typeof(EditableBeatmapSelectionView), nameof(EditableBeatmapSelectionView.SetBeatmap))]
        public bool EditableBeatmapSelectionView_SetBeatmap(ref EditableBeatmapSelectionView __instance, in BeatmapKey beatmapKey)
        {
            if (_mpPlayersDataModel == null) return false;
            if (!beatmapKey.IsValid()) return true;
            if (_beatmapLevelsModel.GetBeatmapLevel(beatmapKey.levelId) != null) return true;

            var levelHash = Utilities.HashForLevelID(beatmapKey.levelId);
            if (string.IsNullOrWhiteSpace(levelHash)) return true;

            var packet = _mpPlayersDataModel.FindLevelPacket(levelHash!);
            if (packet == null) return true;

            __instance._clearButton.gameObject.SetActive(__instance.showClearButton);
            __instance._noLevelText.enabled = false;
            __instance._levelBar.hide = false;

            var level = _mpBeatmapLevelProvider.GetBeatmapFromPacket(packet).MakeBeatmapLevel(beatmapKey, _mpBeatmapLevelProvider.MakeBeatSaverPreviewMediaData(packet.levelHash));

			Plugin.Logger.Debug($"Calling Setup/SetupData with level type: {level.GetType().Name}, beatmapCharacteristic type: {beatmapKey.beatmapCharacteristic.GetType().Name}, difficulty type: {beatmapKey.difficulty.GetType().Name} ");
			if (_newlbarInfo)
			{
				_lbarInfo.Invoke(__instance._levelBar, new object[] { level, beatmapKey.difficulty, beatmapKey.beatmapCharacteristic });
			}
			else
			{
				_lbarInfo.Invoke(__instance._levelBar, new object[] { level, beatmapKey.beatmapCharacteristic, beatmapKey.difficulty });
			}
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
            if (string.IsNullOrWhiteSpace(levelHash)) return true;

            var packet = _mpPlayersDataModel.FindLevelPacket(levelHash!);
            if (packet == null) return true;

            __instance._noLevelText.enabled = false;
            __instance._levelBar.hide = false;

            var level = _mpBeatmapLevelProvider.GetBeatmapFromPacket(packet).MakeBeatmapLevel(beatmapKey, _mpBeatmapLevelProvider.MakeBeatSaverPreviewMediaData(packet.levelHash));

            Plugin.Logger.Debug($"Calling Setup/SetupData with level type: {level.GetType().Name}, beatmapCharacteristic type: {beatmapKey.beatmapCharacteristic.GetType().Name}, difficulty type: {beatmapKey.difficulty.GetType().Name} ");
            if (_newlbarInfo)
            {
	            _lbarInfo.Invoke(__instance._levelBar, new object[] { level, beatmapKey.difficulty, beatmapKey.beatmapCharacteristic });
            }
            else
            {
	            _lbarInfo.Invoke(__instance._levelBar, new object[] { level, beatmapKey.beatmapCharacteristic, beatmapKey.difficulty });
            }
			return false;
        }
    }

	internal static class PacketExt
    {
        public static BeatmapLevel MakeBeatmapLevel(this MpBeatmap mpBeatmap, in BeatmapKey key, IPreviewMediaData previewMediaData) 
        {
            var dict = new Dictionary<(BeatmapCharacteristicSO, BeatmapDifficulty), BeatmapBasicData>
			{
				[(key.beatmapCharacteristic, key.difficulty)] = new BeatmapBasicData(
				0,
				0,
				EnvironmentName.Empty,
				null,
				0,
				0,
				0,
				new[] { mpBeatmap.LevelAuthorName },
				Array.Empty<string>()
			)
			};

			// For 1.35
			var conInfo = AccessTools.Constructor(typeof(BeatmapLevel), new Type[]
			{
				typeof(bool),
				typeof(string),
				typeof(string),
				typeof(string),
				typeof(string),
				typeof(string[]),
				typeof(string[]),
				typeof(float),
				typeof(float),
				typeof(float),
				typeof(float),
				typeof(float),
				typeof(float),
				typeof(PlayerSensitivityFlag),
				typeof(IPreviewMediaData),
				typeof(IReadOnlyDictionary<(BeatmapCharacteristicSO, BeatmapDifficulty), BeatmapBasicData>)
			});
			if (conInfo != null)
			{
				return (BeatmapLevel)conInfo.Invoke(new object[]
				{
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
				});
			}
			// For 1.37
			conInfo = AccessTools.Constructor(typeof(BeatmapLevel), new Type[]
			{
				typeof(int),
				typeof(bool),
				typeof(string),
				typeof(string),
				typeof(string),
				typeof(string),
				typeof(string[]),
				typeof(string[]),
				typeof(float),
				typeof(float),
				typeof(float),
				typeof(float),
				typeof(float),
				typeof(float),
				typeof(PlayerSensitivityFlag),
				typeof(IPreviewMediaData),
				typeof(IReadOnlyDictionary<(BeatmapCharacteristicSO, BeatmapDifficulty), BeatmapBasicData>)
			});
			if (conInfo != null)
				return (BeatmapLevel)conInfo.Invoke(new object[]
				{
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
				});
			throw new NotSupportedException("Game Version not supported");
        }
    }
}
