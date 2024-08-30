using HarmonyLib;
using MultiplayerCore.Beatmaps.Abstractions;
using MultiplayerCore.Beatmaps.Providers;
using MultiplayerCore.Objects;
using SiraUtil.Affinity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using MultiplayerCore.Beatmaps.Packets;

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

            _lbarInfo = AccessTools.Method(typeof(LevelBar), "Setup",
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
        public bool EditableBeatmapSelectionView_SetBeatmap(EditableBeatmapSelectionView __instance, in BeatmapKey beatmapKey)
        {
            if (_mpPlayersDataModel == null) return false;
            if (!beatmapKey.IsValid()) return true;
            if (_beatmapLevelsModel.GetBeatmapLevel(beatmapKey.levelId) != null) return true;

            var levelHash = Utilities.HashForLevelID(beatmapKey.levelId);
            if (string.IsNullOrWhiteSpace(levelHash)) return true;

            var packet = _mpPlayersDataModel.FindLevelPacket(levelHash!);

            __instance.StartCoroutine(SetBeatmapCoroutine(__instance, beatmapKey, levelHash!, packet));
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

            __instance.StartCoroutine(SetBeatmapCoroutine(__instance, beatmapKey, levelHash!, packet));
			return false;
        }

        IEnumerator SetBeatmapCoroutine(BeatmapSelectionView instance, BeatmapKey key, string levelHash, MpBeatmapPacket? packet = null)
        {
	        BeatmapLevel? level;
	        if (packet != null)
	        {
		        level = _mpBeatmapLevelProvider.GetBeatmapFromPacket(packet).MakeBeatmapLevel(key,
			        _mpBeatmapLevelProvider.MakeBeatSaverPreviewMediaData(packet.levelHash));
	        }
	        else
	        {
		        var levelTask = _mpBeatmapLevelProvider.GetBeatmap(levelHash!);
		        yield return IPA.Utilities.Async.Coroutines.WaitForTask(levelTask);

				level = levelTask.Result?.MakeBeatmapLevel(key,
			        _mpBeatmapLevelProvider.MakeBeatSaverPreviewMediaData(levelHash!));
	        }

	        if (level != null)
	        {
		        if (instance is EditableBeatmapSelectionView editView) editView._clearButton.gameObject.SetActive(editView.showClearButton);
		        instance._noLevelText.enabled = false;
		        instance._levelBar.hide = false;


		        Plugin.Logger.Debug($"Calling Setup with level type: {level.GetType().Name}, beatmapCharacteristic type: {key.beatmapCharacteristic.GetType().Name}, difficulty type: {key.difficulty.GetType().Name} ");
		        if (_newlbarInfo)
		        {
			        _lbarInfo.Invoke(instance._levelBar, new object[] { level, key.difficulty, key.beatmapCharacteristic });
		        }
		        else
		        {
			        _lbarInfo.Invoke(instance._levelBar, new object[] { level, key.beatmapCharacteristic, key.difficulty });
		        }
	        }
	        else
	        {
		        Plugin.Logger.Error($"Could not get level info for level {levelHash}");
				if (instance is EditableBeatmapSelectionView editView) editView._clearButton.gameObject.SetActive(false);
				instance._noLevelText.enabled = true;
		        instance._levelBar.hide = true;
	        }
        }
	}

	public static class PacketExt
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
