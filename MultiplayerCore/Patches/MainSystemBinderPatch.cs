using HarmonyLib;
using MultiplayerCore.Objects;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Zenject;

namespace MultiplayerCore.Patches
{
    [HarmonyPatch(typeof(MainSystemInit), nameof(MainSystemInit.InstallBindings), MethodType.Normal)]
    internal class MainSystemBinderPatch
    {
        private static readonly MethodInfo _rootMethod = typeof(FromBinder).GetMethod(nameof(FromBinder.FromComponentInNewPrefab), new[] { typeof(UnityEngine.Object) });

        private static readonly MethodInfo _entitlementCheckerAttacher = SymbolExtensions.GetMethodInfo(() => EntitlementCheckerAttacher(null!, null!));
        private static readonly FieldInfo _entitlementCheckerPrefab = typeof(MainSystemInit).GetField("_networkPlayerEntitlementCheckerPrefab", BindingFlags.NonPublic | BindingFlags.Instance);

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldfld && codes[i].OperandIs(_entitlementCheckerPrefab))
                {
                    if (codes[i + 1].opcode == OpCodes.Callvirt && codes[i + 1].Calls(_rootMethod))
                    {
                        CodeInstruction newCode = new CodeInstruction(OpCodes.Callvirt, _entitlementCheckerAttacher);
                        codes[i + 1] = newCode;
                    }
                }
            }

            return codes.AsEnumerable();
        }

        private static ScopeConcreteIdArgConditionCopyNonLazyBinder EntitlementCheckerAttacher(ConcreteIdBinderGeneric<NetworkPlayerEntitlementChecker> contract, UnityEngine.Object prefab)
        {
            return contract.To<MpEntitlementChecker>().FromNewComponentOnRoot();
        }
    }
}
