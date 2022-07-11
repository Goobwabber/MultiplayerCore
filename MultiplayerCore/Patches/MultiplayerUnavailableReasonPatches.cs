using HarmonyLib;
using Hive.Versioning;
using IPA.Loader;
using MultiplayerCore.Models;

namespace MultiplayerCore.Patches
{
    [HarmonyPatch]
    public class MultiplayerUnavailableReasonPatches
    {
        private static string _requiredMod = string.Empty;
        private static string _requiredVersion = string.Empty;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MultiplayerUnavailableReasonMethods), nameof(MultiplayerUnavailableReasonMethods.TryGetMultiplayerUnavailableReason))]
        private static bool TryGetMultiplayerUnavailableReason(MultiplayerStatusData data, out MultiplayerUnavailableReason reason, ref bool __result)
        {
            reason = (MultiplayerUnavailableReason)0;
            if (data is MpStatusData mpData && mpData.requiredMods != null)
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
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MultiplayerUnavailableReasonMethods), nameof(MultiplayerUnavailableReasonMethods.LocalizedKey))]
        private static bool LocalizeMultiplayerUnavailableReason(MultiplayerUnavailableReason multiplayerUnavailableReason, ref string __result)
        {
            if (multiplayerUnavailableReason != (MultiplayerUnavailableReason)5)
                return true;
            var metadata = PluginManager.GetPluginFromId(_requiredMod);
            __result = $"Mod {metadata.Name} is out of date. Please update to version {_requiredVersion} or newer.";
            return false;
        }
    }
}
