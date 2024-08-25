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
using MultiplayerCore.Beatmaps.Providers;
using MultiplayerCore.Objects;
using MultiplayerCore.Patchers;

namespace MultiplayerCore.Patches
{
	[HarmonyPatch]
	internal class NoLevelSpectatorPatch
	{
		internal static MpBeatmapLevelProvider? mpBeatmapLevelProvider;

		[HarmonyPrefix]
		[HarmonyPatch(typeof(LobbyGameStateController), nameof(LobbyGameStateController.StartMultiplayerLevel))]
		internal static bool LobbyGameStateController_StartMultiplayerLevel(LobbyGameStateController __instance, ILevelGameplaySetupData gameplaySetupData, IBeatmapLevelData beatmapLevelData, Action beforeSceneSwitchCallback)
		{
			mpBeatmapLevelProvider = ((MpPlayersDataModel)__instance._lobbyPlayersDataModel)._beatmapLevelProvider;

			var levelHash = Utilities.HashForLevelID(gameplaySetupData.beatmapKey.levelId);
			if (gameplaySetupData != null && beatmapLevelData == null && !string.IsNullOrWhiteSpace(levelHash))
			{
				Plugin.Logger.Info($"No LevelData for custom level {levelHash} running patch for spectator");
				var levelTask = mpBeatmapLevelProvider.GetBeatmap(levelHash);
				__instance.countdownStarted = false;
				__instance.StopListeningToGameStart(); // Ensure we stop listening for the start event while we run our start task
				levelTask.ContinueWith(beatmapTask =>
				{
					if (__instance.countdownStarted) return;  // Another countdown has started, don't start the level

					BeatmapLevel? beatmapLevel = beatmapTask.Result?.MakeBeatmapLevel(gameplaySetupData.beatmapKey,
						mpBeatmapLevelProvider.MakeBeatSaverPreviewMediaData(levelHash));
					if (beatmapLevel == null)
						beatmapLevel = new NoInfoBeatmapLevel(levelHash).MakeBeatmapLevel(gameplaySetupData.beatmapKey,
							mpBeatmapLevelProvider.MakeBeatSaverPreviewMediaData(levelHash));

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
	}

	[HarmonyPatch]
	internal class NoLevelSpectatorOptionalPatch
	{
		private static MethodInfo _lbarInfo;
		private static bool _newlbarInfo;
		static bool Prepare()
		{
			_lbarInfo = AccessTools.Method(typeof(LevelBar), "Setup",
				new Type[] { typeof(BeatmapLevel), typeof(BeatmapDifficulty), typeof(BeatmapCharacteristicSO) });
			if (_lbarInfo != null) _newlbarInfo = true;
			else _lbarInfo = AccessTools.Method(typeof(LevelBar), "Setup", new Type[] { typeof(BeatmapLevel), typeof(BeatmapCharacteristicSO), typeof(BeatmapDifficulty) });
			if (_lbarInfo == null)
			{
				Plugin.Logger.Critical("Can't find a fitting LevelBar Method, is your game version supported?");
				return false;
			}

			return true;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(MultiplayerResultsViewController), nameof(MultiplayerResultsViewController.Init))]
		internal static void MultiplayerResultsViewController_Init(MultiplayerResultsViewController __instance, BeatmapKey beatmapKey)
		{
			var hash = Utilities.HashForLevelID(beatmapKey.levelId);
			if (NoLevelSpectatorPatch.mpBeatmapLevelProvider != null && !string.IsNullOrWhiteSpace(hash) &&
			    SongCore.Loader.GetLevelByHash(hash) == null)
			{
				IPA.Utilities.Async.UnityMainThreadTaskScheduler.Factory.StartNew<Task>(async () =>
				{
					BeatmapLevel? beatmapLevel = (await NoLevelSpectatorPatch.mpBeatmapLevelProvider.GetBeatmap(hash))?.MakeBeatmapLevel(beatmapKey, NoLevelSpectatorPatch.mpBeatmapLevelProvider.MakeBeatSaverPreviewMediaData(hash));
					if (beatmapLevel == null)
						beatmapLevel = new NoInfoBeatmapLevel(hash).MakeBeatmapLevel(beatmapKey, NoLevelSpectatorPatch.mpBeatmapLevelProvider.MakeBeatSaverPreviewMediaData(hash));
					Plugin.Logger.Trace($"Calling Setup with level type: {beatmapLevel.GetType().Name}, beatmapCharacteristic type: {beatmapKey.beatmapCharacteristic.GetType().Name}, difficulty type: {beatmapKey.difficulty.GetType().Name} ");
					if (_newlbarInfo)
					{
						_lbarInfo.Invoke(__instance._levelBar, new object[] { beatmapLevel, beatmapKey.difficulty, beatmapKey.beatmapCharacteristic });
					}
					else
					{
						_lbarInfo.Invoke(__instance._levelBar, new object[] { beatmapLevel, beatmapKey.beatmapCharacteristic, beatmapKey.difficulty });
					}
				});
			}
		}
	}
}
