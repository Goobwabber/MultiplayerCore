using HarmonyLib;
using Hive.Versioning;
using IPA.Loader;
using IPA.Utilities;
using MultiplayerCore.Models;

namespace MultiplayerCore.Patches
{
    [HarmonyPatch]
    internal class MultiplayerUnavailableReasonPatches
    {
        private static string _requiredMod = string.Empty;
        private static string _requiredVersion = string.Empty;
        private static string _maximumBsVersion = string.Empty;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MultiplayerUnavailableReasonMethods), nameof(MultiplayerUnavailableReasonMethods.TryGetMultiplayerUnavailableReason))]
        private static bool TryGetMultiplayerUnavailableReasonPrefix(MultiplayerStatusData data, out MultiplayerUnavailableReason reason, ref bool __result)
        {
            reason = (MultiplayerUnavailableReason)0;
            if (data is MpStatusData mpData)
            {
                if (mpData.requiredMods != null)
                {
                    foreach (var requiredMod in mpData.requiredMods)
                    {
                        var metadata = PluginManager.GetPluginFromId(requiredMod.id);
                        if (metadata == null && !requiredMod.required)
                            // Optional mod is not installed
                            continue;

                        var requiredVersion = new Version(requiredMod.version);
                        if (metadata != null && metadata.HVersion >= requiredVersion)
                            // Mod is installed and up to date
                            continue;

                        reason = (MultiplayerUnavailableReason)5;
                        _requiredMod = requiredMod.id;
                        _requiredVersion = requiredMod.version;
                        __result = true;
                        return false;
                    }
                }

                if (mpData.maximumAppVersion != null)
                {
                    var version = new AlmostVersion(mpData.maximumAppVersion);
                    if (UnityGame.GameVersion > version)
                    {
                        reason = (MultiplayerUnavailableReason)6;
                        _maximumBsVersion = mpData.maximumAppVersion;
                        __result = true;
                        return false;
                    }
                }
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MultiplayerUnavailableReasonMethods), nameof(MultiplayerUnavailableReasonMethods.LocalizedKey))]
        private static bool LocalizeMultiplayerUnavailableReason(MultiplayerUnavailableReason multiplayerUnavailableReason, ref string __result)
        {
            if (multiplayerUnavailableReason == (MultiplayerUnavailableReason)5)
            {
                var metadata = PluginManager.GetPluginFromId(_requiredMod);
                __result = $"Multiplayer Unavailable\nMod {metadata.Name} is missing or out of date\nPlease install version {_requiredVersion} or newer";
                return false;
            }
            if (multiplayerUnavailableReason == (MultiplayerUnavailableReason)6)
            {
                __result = $"Multiplayer Unavailable\nBeat Saber version is too new\nMaximum version: {_maximumBsVersion}\nCurrent version: {UnityGame.GameVersion}";
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ConnectionFailedReasonMethods), nameof(ConnectionFailedReasonMethods.LocalizedKey))]
        private static bool LocalizeConnectionFailedReason(ConnectionFailedReason connectionFailedReason,
	        ref string __result)
        {
	        if (connectionFailedReason == (ConnectionFailedReason)50)
	        {
                //__result = "CONNECTION_FAILED_VERSION_MISMATCH"; // Would show an "Update the game message"
                __result =
                 $"Game Version Unknown\n" +
                 $"Your game version was not within any version ranges known by the server";
                return false;
	        }
	        if (connectionFailedReason == (ConnectionFailedReason)51)
	        {
		        __result =
			        $"Game Version Too Old\n" +
			        $"Your game version is below the supported version range of the lobby\n" +
			        $"You either need to update or the lobby host needs to downgrade their game";
		        return false;
	        }
	        if (connectionFailedReason == (ConnectionFailedReason)52)
	        {
		        __result =
			        $"Game Version Too New\n" +
			        $"Your game version is above the supported version range of the lobby\n" +
			        $"You either need to downgrade or the lobby host needs to update their game";
		        return false;
	        }

			return true;
        }

		[HarmonyPrefix]
        [HarmonyPatch(typeof(MultiplayerPlacementErrorCodeMethods), nameof(MultiplayerPlacementErrorCodeMethods.ToConnectionFailedReason))]
        private static bool ToConnectionFailedReason(MultiplayerPlacementErrorCode errorCode,
	        ref ConnectionFailedReason __result)
        {
            Plugin.Logger.Debug($"Got MPEC-{errorCode}");
	        if ((int)errorCode >= 50)
	        {
		        __result = (ConnectionFailedReason)errorCode;
		        return false;
	        }

	        return true;
        }

	}
}
