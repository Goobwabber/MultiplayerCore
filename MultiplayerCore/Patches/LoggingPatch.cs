using HarmonyLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace MultiplayerCore.Patches
{
    [HarmonyPatch]
    internal class LoggingPatch
    {
        [HarmonyTranspiler]
        [HarmonyPatch(typeof(ConnectedPlayerManager), "HandleNetworkReceive")]
        private static IEnumerable<CodeInstruction> PacketErrorLogger(IEnumerable<CodeInstruction> instructions, ILGenerator gen)
        {
            LocalBuilder localException = gen.DeclareLocal(typeof(Exception));
            localException.SetLocalSymInfo("ex");

            foreach (CodeInstruction? code in instructions)
            {
                if (code.opcode == OpCodes.Pop)
                {
                    CodeInstruction current = new CodeInstruction(OpCodes.Stloc, localException);
                    current.blocks.Add(new ExceptionBlock(ExceptionBlockType.BeginCatchBlock));
                    yield return current; // Store exception in local
                    current = new CodeInstruction(OpCodes.Ldarg_2);
                    yield return current; // Load packet onto stack
                    current = new CodeInstruction(OpCodes.Ldloc_3);
                    yield return current; // Load player onto stack
                    current = new CodeInstruction(OpCodes.Ldloc, localException);
                    yield return current; // Load exception onto stack
                    current = new CodeInstruction(OpCodes.Callvirt, SymbolExtensions.GetMethodInfo(() => LogPacketError(null!, null!, null!)));
                    yield return current;
                } 
                else
                {
                    yield return code;
                }
            }
        }

        private static void LogPacketError(NetDataReader reader, IConnectedPlayer p, Exception ex)
        {
            Plugin.Logger.Warn($"An exception was thrown processing a packet from player '{p?.userName ?? "<NULL>"}|{p?.userId ?? " < NULL > "}': {ex.Message}");
            Plugin.Logger.Debug(ex);
#if (DEBUG)
            Plugin.Logger.Error($"Errored packet: {BitConverter.ToString(reader.RawData)}");
#endif
        }
    }
}
