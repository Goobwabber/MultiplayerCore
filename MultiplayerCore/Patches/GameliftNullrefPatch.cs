using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace MultiplayerCore.Patches
{
    [HarmonyPatch]
    internal static class GameliftNullrefPatch
    {
        static MethodBase TargetMethod() =>
            AccessTools.FirstInner(typeof(MultiplayerModeSelectionFlowCoordinator), t => t.Name.StartsWith("<TryShowModeSelection"))?.GetMethod("MoveNext", BindingFlags.NonPublic | BindingFlags.Instance)!;

        private static readonly MethodInfo _useGameliftAttacher = SymbolExtensions.GetMethodInfo(() => UseGameliftAttacher(null!));

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) =>
            new CodeMatcher(instructions)
                .MatchForward(false, new CodeMatch(i => i.opcode == OpCodes.Ldfld && ((FieldInfo)i.operand).Name == "useGamelift"))
                .Set(OpCodes.Callvirt, _useGameliftAttacher)
                .InstructionEnumeration();

        private static bool UseGameliftAttacher(MultiplayerStatusData status)
            => status?.useGamelift ?? false;
    }
}
