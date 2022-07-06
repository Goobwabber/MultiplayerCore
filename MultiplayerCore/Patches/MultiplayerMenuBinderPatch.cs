using HarmonyLib;
using MultiplayerCore.Objects;
using SiraUtil.Affinity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Zenject;

namespace MultiplayerCore.Patches
{
    [HarmonyPatch(typeof(MultiplayerMenuInstaller), nameof(MultiplayerMenuInstaller.InstallBindings), MethodType.Normal)]
    internal class MultiplayerMenuBinderPatch
    {
        private static readonly MethodInfo _rootMethod = typeof(DiContainer).GetMethod(nameof(DiContainer.BindInterfacesAndSelfTo), Array.Empty<Type>());

        private static readonly MethodInfo _levelLoaderAttacher = SymbolExtensions.GetMethodInfo(() => LevelLoaderAttacher(null!));
        private static readonly MethodInfo _levelLoaderMethod = _rootMethod.MakeGenericMethod(new Type[] { typeof(MultiplayerLevelLoader) });

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Callvirt && codes[i].Calls(_levelLoaderMethod))
                {
                    CodeInstruction newCode = new CodeInstruction(OpCodes.Callvirt, _levelLoaderAttacher);
                    codes[i] = newCode;
                }
            }

            return codes.AsEnumerable();
        }

        private static FromBinderNonGeneric LevelLoaderAttacher(DiContainer contract)
        {
            return contract.Bind(typeof(MultiplayerLevelLoader), typeof(MpLevelLoader), typeof(ITickable)).To<MpLevelLoader>();
        }
    }
}
