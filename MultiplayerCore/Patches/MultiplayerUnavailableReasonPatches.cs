using HarmonyLib;
using Hive.Versioning;
using IPA.Loader;
using IPA.Utilities;
using MultiplayerCore.Models;
using System.Windows.Forms;

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
                        if (metadata == null)
                            continue;

                        var requiredVersion = new Version(requiredMod.version);
                        if (requiredVersion <= metadata.HVersion)
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
                __result = $"Multiplayer Unavailable\nMod {metadata.Name} is out of date.\nPlease update to version {_requiredVersion} or newer.";
                return false;
            } else if (multiplayerUnavailableReason == (MultiplayerUnavailableReason)6)
            {
                __result = $"Multiplayer Unavailable\nBeat Saber version is too new\nMaximum version: {_maximumBsVersion}\nCurrent version: {UnityGame.GameVersion.ToString()}";
                return false;
            }
            return true;
        }
    }
}
