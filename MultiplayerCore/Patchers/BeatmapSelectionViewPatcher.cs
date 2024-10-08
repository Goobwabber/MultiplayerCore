﻿using HarmonyLib;
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
        private MpPlayersDataModel? _mpPlayersDataModel;
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

				instance._levelBar.Setup(level, key.difficulty, key.beatmapCharacteristic);
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
				0,
				new[] { mpBeatmap.LevelAuthorName },
				Array.Empty<string>()
			)
			};

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
