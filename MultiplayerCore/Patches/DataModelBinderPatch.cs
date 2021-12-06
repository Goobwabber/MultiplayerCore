using HarmonyLib;
using MultiplayerCore.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Zenject;

namespace MultiplayerCore.Patches
{
    [HarmonyPatch(typeof(LobbyDataModelInstaller), nameof(LobbyDataModelInstaller.InstallBindings))]
    internal class DataModelBinderPatch
    {
        private static readonly MethodInfo _rootMethod = typeof(ConcreteBinderNonGeneric).GetMethod(nameof(ConcreteBinderNonGeneric.To), Array.Empty<Type>());

        private static readonly MethodInfo _playersDataModelAttacher = SymbolExtensions.GetMethodInfo(() => PlayersDataModelAttacher(null!));
        private static readonly MethodInfo _playersDataModelMethod = _rootMethod.MakeGenericMethod(new Type[] { typeof(LobbyPlayersDataModel) });

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Callvirt)
                {
                    if (codes[i].Calls(_playersDataModelMethod))
                    {
                        CodeInstruction newCode = new CodeInstruction(OpCodes.Callvirt, _playersDataModelAttacher);
                        codes[i] = newCode;
                    }
                }
            }

            return codes.AsEnumerable();
        }

        private static FromBinderNonGeneric PlayersDataModelAttacher(ConcreteBinderNonGeneric contract)
        {
            return contract.To<MpPlayersDataModel>();
        }
    }
}
