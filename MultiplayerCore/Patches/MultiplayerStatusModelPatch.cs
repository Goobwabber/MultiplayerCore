using HarmonyLib;
using MultiplayerCore.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace MultiplayerCore.Patches
{
    [HarmonyPatch]
    internal class MultiplayerStatusModelPatch
    {
        static MethodBase TargetMethod() =>
            AccessTools.FirstInner(typeof(MultiplayerStatusModel), t => t.Name.StartsWith("<GetMultiplayerStatusAsyncInternal"))?.GetMethod("MoveNext", BindingFlags.NonPublic | BindingFlags.Instance)!;

        private static readonly MethodInfo _deserializeObjectMethod = SymbolExtensions.GetMethodInfo(() => JsonUtility.FromJson<MultiplayerStatusData>(null!));
        private static readonly MethodInfo _deserializeObjectAttacher = SymbolExtensions.GetMethodInfo(() => DeserializeObjectAttacher(null!));

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) =>
            new CodeMatcher(instructions)
                .MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Call && i.Calls(_deserializeObjectMethod)))
                .Set(OpCodes.Call, _deserializeObjectAttacher)
                .InstructionEnumeration();

        private static object DeserializeObjectAttacher(string value)
            => JsonUtility.FromJson<MpStatusData>(value);
    }
}
