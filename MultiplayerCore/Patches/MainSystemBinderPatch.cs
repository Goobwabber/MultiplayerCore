using BGNet.Core.GameLift;
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


		private static readonly MethodInfo _bindTypes = typeof(DiContainer).GetMethod(nameof(DiContainer.Bind), new[] { typeof(System.Type[]) });
		private static readonly MethodInfo _toSessionProvider = typeof(ConcreteBinderNonGeneric).GetMethods()
	        .First(m => m.Name == "To" && m.IsGenericMethodDefinition).MakeGenericMethod(new[] { typeof(GameLiftPlayerSessionProvider) });
		private static readonly MethodInfo _sessionProviderAttacher = SymbolExtensions.GetMethodInfo(() => PlayerSessionProviderAttacher(null!));

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            for (int i = 0; i < codes.Count; i++)
            {
				Plugin.Logger.Trace($"Instruction at index {i}: {codes[i].opcode}");
				if (codes[i].opcode == OpCodes.Ldfld && codes[i].OperandIs(_entitlementCheckerPrefab))
                {
					Plugin.Logger.Trace($"Ldfld: {codes[i].operand}");
					if (codes[i + 1].opcode == OpCodes.Callvirt && codes[i + 1].Calls(_rootMethod))
                    {
						Plugin.Logger.Trace($"Callvirt method: {codes[i + 1].operand}");
						Plugin.Logger.Trace("Patched EntitlementChecker prefab");
                        CodeInstruction newCode = new CodeInstruction(OpCodes.Callvirt, _entitlementCheckerAttacher);
                        codes[i + 1] = newCode;
                    }
                } else if ((i + 2) < codes.Count && codes[i].opcode == OpCodes.Stelem_Ref)
                {
                    if (codes[i + 1].opcode == OpCodes.Callvirt && codes[i + 1].Calls(_bindTypes))
                    {
						Plugin.Logger.Trace($"Callvirt 1 method: {codes[i + 1].operand}");
						if (codes[i + 2].opcode == OpCodes.Callvirt && codes[i + 2].Calls(_toSessionProvider))
						{
							Plugin.Logger.Trace($"Callvirt 2 method: {codes[i + 2].operand}");
							Plugin.Logger.Trace("Patched IGameLiftPlayerSessionProvider");
							CodeInstruction replaceCode = new CodeInstruction(OpCodes.Callvirt, _sessionProviderAttacher);
							codes[i + 2] = replaceCode;
						}
					}
				} else if (codes[i].opcode == OpCodes.Callvirt)
                {
					Plugin.Logger.Trace($"Callvirt method: {codes[i].operand}");
				}
			}

            return codes.AsEnumerable();
        }

        private static ScopeConcreteIdArgConditionCopyNonLazyBinder EntitlementCheckerAttacher(ConcreteIdBinderGeneric<NetworkPlayerEntitlementChecker> contract, UnityEngine.Object prefab)
        {
            return contract.To<MpEntitlementChecker>().FromNewComponentOnRoot();
        }

        private static FromBinderGeneric<CustomPlayerSessionProvider> PlayerSessionProviderAttacher(ConcreteBinderGeneric<GameLiftPlayerSessionProvider> contract)
        {
           return contract.To<CustomPlayerSessionProvider>();
        }
	}
}
