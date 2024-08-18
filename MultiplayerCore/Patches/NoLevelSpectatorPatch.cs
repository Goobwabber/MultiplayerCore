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
using Zenject;

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
		static bool Prepare()
		{
			return UnityGame.GameVersion >= new AlmostVersion("1.37.0");
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(LevelBar), nameof(LevelBar.Setup), new Type[] { typeof(BeatmapKey) })]
		internal static bool LevelBar_Setup(LevelBar __instance, BeatmapKey beatmapKey)
		{
			var hash = Utilities.HashForLevelID(beatmapKey.levelId);
			if (NoLevelSpectatorPatch.mpBeatmapLevelProvider != null && !string.IsNullOrWhiteSpace(hash) && SongCore.Loader.GetLevelByHash(hash) == null)
			{
				IPA.Utilities.Async.UnityMainThreadTaskScheduler.Factory.StartNew<Task>(async () =>
				{
					BeatmapLevel? beatmapLevel = (await NoLevelSpectatorPatch.mpBeatmapLevelProvider.GetBeatmap(hash))?.MakeBeatmapLevel(beatmapKey, NoLevelSpectatorPatch.mpBeatmapLevelProvider.MakeBeatSaverPreviewMediaData(hash));
					if (beatmapLevel == null)
						beatmapLevel = new NoInfoBeatmapLevel(hash).MakeBeatmapLevel(beatmapKey, NoLevelSpectatorPatch.mpBeatmapLevelProvider.MakeBeatSaverPreviewMediaData(hash));

					__instance.SetupData(beatmapLevel, beatmapKey.difficulty, beatmapKey.beatmapCharacteristic);

				});
				return false;
			}

			return true;
		}
	}
}
