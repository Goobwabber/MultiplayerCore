using HarmonyLib;
using MultiplayerCore.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiplayerCore.Patches.OverridePatches
{
	[HarmonyPatch(typeof(LobbyPlayersDataModel))]
	internal class PlayersDataModelOverride
	{

		[HarmonyPrefix]
		[HarmonyPatch(nameof(LobbyPlayersDataModel.SetPlayerBeatmapLevel))]
		private static void SetPlayerBeatmapLevel_override(LobbyPlayersDataModel __instance, string userId, in BeatmapKey beatmapKey)
		{
			Plugin.Logger.Debug("Called LobbyPlayersDataModel.SetPlayerBeatmapLevel Override Patch");
			((MpPlayersDataModel)__instance).SetPlayerBeatmapLevel_override(userId, beatmapKey);
		}
	}
}
