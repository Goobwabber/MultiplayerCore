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
		[HarmonyPatch(nameof(LobbyPlayersDataModel.SetLocalPlayerBeatmapLevel))]
		private static void SetLocalPlayerBeatmapLevel_override(LobbyPlayersDataModel __instance, in BeatmapKey beatmapKey)
		{
			Plugin.Logger.Debug("Called LobbyPlayersDataModel.SetLocalPlayerBeatmapLevel Override Patch");
			((MpPlayersDataModel)__instance).SetLocalPlayerBeatmapLevel_override(beatmapKey);
		}
	}
}
