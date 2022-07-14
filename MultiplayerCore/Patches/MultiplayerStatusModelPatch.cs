using HarmonyLib;
using MultiplayerCore.Models;
using Newtonsoft.Json;
using System;

namespace MultiplayerCore.Patches
{
    [HarmonyPatch]
    public class MultiplayerStatusModelPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(JsonConvert), nameof(JsonConvert.DeserializeObject), new[] {typeof(string), typeof(Type), typeof(JsonSerializerSettings)})]
        private static void DeserializeObject(ref Type type) {
            if (type == typeof(MultiplayerStatusData))
                type = typeof(MpStatusData);
        }
    }
}
