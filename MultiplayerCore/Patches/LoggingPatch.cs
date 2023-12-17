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
#if !DEBUG
                [HarmonyTranspiler]
                [HarmonyPatch(typeof(ConnectedPlayerManager), nameof(ConnectedPlayerManager.HandleNetworkReceive))]
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
//#if DEBUG
//                    Plugin.Logger.Error($"Errored packet: {BitConverter.ToString(reader.RawData)}");
//#endif
                }

#else
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ConnectedPlayerManager), nameof(ConnectedPlayerManager.HandleNetworkReceive))]
        private static bool HandleNetworkReceive(IConnection connection, NetDataReader reader, BGNet.Core.DeliveryMethod deliveryMethod, ref ConnectedPlayerManager __instance)
        {
            byte b, b3 = 0;
            byte b2 = 0;
            if (!reader.TryGetByte(out b) || !reader.TryGetByte(out b2) || !reader.TryGetByte(out b3) || reader.AvailableBytes == 0)
            {
                Plugin.Logger.Warn("Received invalid packet");
                Plugin.Logger.Debug($"{b}-{b2}-{b3} AvailableBytes = {reader.AvailableBytes}");
                return false;
            }
            ConnectedPlayerManager.ConnectedPlayer player = __instance.GetPlayer(connection, b);
            if (player == null)
            {
                Plugin.Logger.Debug("Player is us ignoring packet");
                return false;
            }
            bool flag = (b3 & 1) == 1;
            b2 &= 127;

            Plugin.Logger.Debug($"Routing Header, {b}-{b2}-{b3}");

            if (b2 == 127 && flag)
            {
                Plugin.Logger.Debug("Packet is not for us ignoring packet");
                return false;
            }
            bool flag2 = (b3 & 2) == 0;
            if (b2 != 0 && __instance._connectionManager.connectionCount > 1 && flag2)
            {
                if (b2 == 127)
                {
                    Plugin.Logger.Debug("Packet is for all sending to all");
                    __instance._temporaryDataWriter.Reset();
                    __instance._temporaryDataWriter.SetUpPacket(player.connectionId, 127, b3);
                    __instance._temporaryDataWriter.Put(reader.RawData, reader.Position, reader.AvailableBytes);
                    __instance._connectionManager.SendToAll(__instance._temporaryDataWriter, deliveryMethod, connection);
                }
                else
                {
                    ConnectedPlayerManager.ConnectedPlayer player2 = __instance.GetPlayer(b2);
                    if (player2 != null && player2.connection != connection)
                    {
                        Plugin.Logger.Debug("Packet is for another player sending to them");
                        __instance._temporaryDataWriter.Reset();
                        __instance._temporaryDataWriter.SetUpPacket(player.connectionId, player2.remoteConnectionId, b3);
                        __instance._temporaryDataWriter.Put(reader.RawData, reader.Position, reader.AvailableBytes);
                        player2.connection.Send(__instance._temporaryDataWriter, deliveryMethod);
                    }
                }
            }
            if (b2 != 0 && b2 != 127)
            {
                Plugin.Logger.Debug("Packet is not for us ignoring packet");
                return false;
            }
            if (flag)
            {
                byte[] rawData = reader.RawData;
                int position = reader.Position;
                int availableBytes = reader.AvailableBytes;
                if (player.encryptionState == null || !player.encryptionState.TryDecryptData(rawData, ref position, ref availableBytes) || availableBytes == 0)
                {
                    Plugin.Logger.Warn("Could not decrypt packet");
                    return false;
                }
                reader.SetSource(rawData, position, availableBytes + position);
            }
            try
            {
                Plugin.Logger.Debug("Packet OK Processing...");
                __instance._messageSerializer.ProcessAllPackets(reader, player);
            }
            catch (Exception ex)
            {
                Plugin.Logger.Warn($"An exception was thrown processing a packet from player '{player?.userName ?? "<NULL>"}|{player?.userId ?? " < NULL > "}': {ex.Message}");
                Plugin.Logger.Debug(ex);
                Plugin.Logger.Error($"Errored packet: {BitConverter.ToString(reader.RawData)}");


                if (__instance.isConnectionOwner)
                {
                    __instance.KickPlayer(player?.userId, DisconnectedReason.Kicked);
                }
            }
            return false;
        }
#endif

    }
}
