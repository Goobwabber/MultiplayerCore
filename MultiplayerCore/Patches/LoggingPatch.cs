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
            Plugin.Logger.Error($"Errored packet Postion={reader.Position}, RawDataSize={reader.RawDataSize} RawData: {BitConverter.ToString(reader.RawData)}");
            try
            {
	            reader.SkipBytes(-reader.Position);
	            byte header1, header2, header3;
	            if (!reader.TryGetByte(out header1) || !reader.TryGetByte(out header2) ||
	                !reader.TryGetByte(out header3) || reader.AvailableBytes == 0)
	            {
		            Plugin.Logger.Debug("Failed to get RoutingHeader");
	            }
	            else
	            {
		            Plugin.Logger.Debug($"Routing Header bytes=({header1},{header2},{header3})");
		            int index = 0;
		            while (!reader.EndOfData && index < 100)
		            {
                        Plugin.Logger.Debug($"Iteration='{index}' Attempt read data length from packet");
			            int length = (int)reader.GetVarUInt();
			            int subIteration = 0;
			            while (length > 0 && length <= reader.AvailableBytes && subIteration < 100)
			            {
				            Plugin.Logger.Debug($"Iteration='{index}' subIteration='{subIteration}' Length='{length}' AvailableBytes={reader.AvailableBytes}");
				            byte packetId = reader.GetByte();
				            length--;
				            Plugin.Logger.Debug($"Iteration='{index}' subIteration='{subIteration}' PacketId='{packetId}' RemainingLength='{length}'");
				            subIteration++;
			            }
			            reader.SkipBytes(Math.Min(length, reader.AvailableBytes));
                        Plugin.Logger.Debug($"Iteration='{index}' RemainingBytes='{reader.AvailableBytes}'");
			            index++;
		            }
	            }
            }
            catch
            {
            }
            finally
            {
	            Plugin.Logger.Debug($"Finished Debug Logging for Packet!");
			}
#endif
		}
    }
}
