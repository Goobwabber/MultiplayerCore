using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using BGLib.Polyglot;
using HarmonyLib;
using IPA.Utilities;
using MultiplayerCore.Beatmaps;
using MultiplayerCore.Beatmaps.Abstractions;
using MultiplayerCore.Beatmaps.Providers;
using MultiplayerCore.Objects;
using MultiplayerCore.Patchers;

namespace MultiplayerCore.Patches
{
	[HarmonyPatch]
	internal class NoLevelSpectatorPatch
	{
		internal static MpBeatmapLevelProvider? _mpBeatmapLevelProvider;
		internal static MpPlayersDataModel? _playersDataModel;

		[HarmonyPrefix]
		[HarmonyPatch(typeof(LobbyGameStateController), nameof(LobbyGameStateController.StartMultiplayerLevel))]
		internal static bool LobbyGameStateController_StartMultiplayerLevel(LobbyGameStateController __instance, ILevelGameplaySetupData gameplaySetupData, IBeatmapLevelData beatmapLevelData, Action beforeSceneSwitchCallback)
		{
			_playersDataModel = __instance._lobbyPlayersDataModel as MpPlayersDataModel;
			_mpBeatmapLevelProvider = _playersDataModel?._beatmapLevelProvider;

			if (_playersDataModel == null || _mpBeatmapLevelProvider == null)
			{
				Plugin.Logger.Critical($"Missing custom MpPlayersDataModel or MpBeatmapLevelProvider, cannot continue, returning...");
				return false;
			}

			var levelHash = Utilities.HashForLevelID(gameplaySetupData.beatmapKey.levelId);
			if (gameplaySetupData != null && beatmapLevelData == null && !string.IsNullOrWhiteSpace(levelHash))
			{
				Plugin.Logger.Info($"No LevelData for custom level {levelHash} running patch for spectator");
				var packet = _playersDataModel.FindLevelPacket(levelHash!);
				Task<MpBeatmap?>? levelTask = null;
				if (packet != null)
				{
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target types.
					levelTask = Task.FromResult(_mpBeatmapLevelProvider.GetBeatmapFromPacket(packet));
#pragma warning restore CS8619
				}

				if (levelTask == null) levelTask = _mpBeatmapLevelProvider.GetBeatmap(levelHash!);
				__instance.countdownStarted = false;
				__instance.StopListeningToGameStart(); // Ensure we stop listening for the start event while we run our start task
				levelTask.ContinueWith(beatmapTask =>
				{
					if (__instance.countdownStarted) return;  // Another countdown has started, don't start the level

					BeatmapLevel? beatmapLevel = beatmapTask.Result?.MakeBeatmapLevel(gameplaySetupData.beatmapKey,
						_mpBeatmapLevelProvider.MakeBeatSaverPreviewMediaData(levelHash!));
					if (beatmapLevel == null)
						beatmapLevel = new NoInfoBeatmapLevel(levelHash!).MakeBeatmapLevel(gameplaySetupData.beatmapKey,
							_mpBeatmapLevelProvider.MakeBeatSaverPreviewMediaData(levelHash!));

					__instance._menuTransitionsHelper.StartMultiplayerLevel("Multiplayer", gameplaySetupData.beatmapKey, beatmapLevel, beatmapLevelData,
						__instance._playerDataModel.playerData.colorSchemesSettings.GetOverrideColorScheme(), gameplaySetupData.gameplayModifiers,
						__instance._playerDataModel.playerData.playerSpecificSettings, null, Localization.Get("BUTTON_MENU"), false,
						beforeSceneSwitchCallback,
						__instance.HandleMultiplayerLevelDidFinish,
						__instance.HandleMultiplayerLevelDidDisconnect
					);
				});
				return false;
			}
			Plugin.Logger.Debug("LevelData present running orig");
			return true;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(MultiplayerResultsViewController), nameof(MultiplayerResultsViewController.Init))]
		internal static void MultiplayerResultsViewController_Init(MultiplayerResultsViewController __instance, BeatmapKey beatmapKey)
		{
			var hash = Utilities.HashForLevelID(beatmapKey.levelId);
			if (NoLevelSpectatorPatch._mpBeatmapLevelProvider != null && !string.IsNullOrWhiteSpace(hash) &&
				SongCore.Loader.GetLevelByHash(hash!) == null)
			{
				IPA.Utilities.Async.UnityMainThreadTaskScheduler.Factory.StartNew<Task>(async () =>
				{
					var packet = NoLevelSpectatorPatch._playersDataModel?.FindLevelPacket(hash!);
					BeatmapLevel? beatmapLevel = packet != null ? NoLevelSpectatorPatch._mpBeatmapLevelProvider.GetBeatmapFromPacket(packet)?
						.MakeBeatmapLevel(beatmapKey, NoLevelSpectatorPatch._mpBeatmapLevelProvider.MakeBeatSaverPreviewMediaData(hash!)) : null;
					if (beatmapLevel == null) beatmapLevel = (await NoLevelSpectatorPatch._mpBeatmapLevelProvider.GetBeatmap(hash!))?
						.MakeBeatmapLevel(beatmapKey, NoLevelSpectatorPatch._mpBeatmapLevelProvider.MakeBeatSaverPreviewMediaData(hash!));
					if (beatmapLevel == null)
						beatmapLevel = new NoInfoBeatmapLevel(hash!).MakeBeatmapLevel(beatmapKey, NoLevelSpectatorPatch._mpBeatmapLevelProvider.MakeBeatSaverPreviewMediaData(hash!));
					Plugin.Logger.Trace($"Calling Setup with level type: {beatmapLevel.GetType().Name}, beatmapCharacteristic type: {beatmapKey.beatmapCharacteristic.GetType().Name}, difficulty type: {beatmapKey.difficulty.GetType().Name} ");
					__instance._levelBar.Setup(beatmapLevel, beatmapKey.difficulty, beatmapKey.beatmapCharacteristic);
				});
			}
		}
	}
}
