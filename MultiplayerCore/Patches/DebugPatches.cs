using HarmonyLib;
using LiteNetLib.Utils;
using MultiplayerCore.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace MultiplayerCore.Patches
{
    [HarmonyPatch]
    internal class DebugPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MultiplayerLobbyConnectionController), nameof(MultiplayerLobbyConnectionController.HandleMultiplayerSessionManagerConnected))]
        private static void SessionManagerConnected()
        {
            Plugin.Logger.Debug("Connecting to Session Manager");
        }

        //[HarmonyPrefix]
        //[HarmonyPatch(typeof(BaseNetworkPlayerModel), nameof(BaseNetworkPlayerModel.CreateConnectedPlayerManager))]
        //private static void CreateConnectedPlayerManager()
        //{
        //    Plugin.Logger.Debug("Creating ConnectedPlayerManager");
        //}

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MultiplayerLobbyConnectionController), nameof(MultiplayerLobbyConnectionController.CreateParty))]
        private static void CreateParty()
        {
            Plugin.Logger.Debug("Creating Party");
        }


        [HarmonyPrefix]
        [HarmonyPatch(typeof(MultiplayerModeSelectionFlowCoordinator), nameof(MultiplayerModeSelectionFlowCoordinator.HandleMultiplayerLobbyConnectionControllerConnectionSuccess))]
        private static void LobbyConnection()
        {
            Plugin.Logger.Debug("Connected to Lobby");
        }

#if DEBUG

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameLiftConnectionManager), nameof(GameLiftConnectionManager.HandleReceivedData))]
        private static void ReceivedData(IConnection connection, NetDataReader reader, BGNet.Core.DeliveryMethod deliveryMethod)
        {
            Plugin.Logger.Debug($"Received Data from {connection.userName} with DeliveryMethod {deliveryMethod}");
//#if DEBUG
            Plugin.Logger.Debug($"Received Data: {BitConverter.ToString(reader.RawData)}");
//#endif
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(LobbyPlayersDataModel), nameof(LobbyPlayersDataModel.HandleMenuRpcManagerSetPlayersPermissionConfiguration))]
        private static void MenuRpcSetPlayersPermissionsConfiguration(string userId, PlayersLobbyPermissionConfigurationNetSerializable playersLobbyPermissionConfiguration)
        {
            Plugin.Logger.Debug($"Got PermissionConfiguration for {userId}, {playersLobbyPermissionConfiguration} null check userId {(userId == null ? "null" : "Not null")} playersLobbyPermissionConfiguration {(playersLobbyPermissionConfiguration == null ? "null" : "Not null")}");
        }




    }

    [HarmonyPatch]
    internal class DebugPatchesPacketProcessor
    {
        static MethodBase TargetMethod() => AccessTools.Method(typeof(NetworkPacketSerializer<ConnectedPlayerManager.InternalMessageType, IConnectedPlayer>), nameof(NetworkPacketSerializer<ConnectedPlayerManager.InternalMessageType, IConnectedPlayer>.ProcessPacketInternal));

        private static bool Prefix(NetDataReader reader, int length, IConnectedPlayer data, ref NetworkPacketSerializer<ConnectedPlayerManager.InternalMessageType, IConnectedPlayer> __instance)
        {
            Plugin.Logger.Debug($"Deserialize Packet available bytes {reader.AvailableBytes} length {length}");
            byte @byte = reader.GetByte();
            length--;
            Action<NetDataReader, int, IConnectedPlayer> action;
            if (__instance._messsageHandlers.TryGetValue(@byte, out action))
            {
                if (action != null)
                {
                    Type? packetType = __instance._typeRegistry.FirstOrDefault(x => x.Value == @byte).Key;

                    Plugin.Logger.Debug($"Found MessageHandler for Packet identifier {@byte} at pos {reader.Position} {(ConnectedPlayerManager.InternalMessageType)@byte} {packetType}");
                    action(reader, length, data);
                    return false;
                }
            }
            else
            {
                Plugin.Logger.Error($"Received unknown packet type {@byte} at pos {reader.Position} from player '{data?.userName ?? "<NULL>"}|{data?.userId ?? " < NULL > "}' skipping bytes {length}");
                reader.SkipBytes(length);
            }
            return false;
        }
    }

    [HarmonyPatch]
    internal class DebugPatchesMultiplayerSessionManagerPacketProcessor
    {
        static MethodBase TargetMethod() => AccessTools.Method(typeof(NetworkPacketSerializer<MultiplayerSessionManager.MessageType, IConnectedPlayer>), nameof(NetworkPacketSerializer<MultiplayerSessionManager.MessageType, IConnectedPlayer>.ProcessPacketInternal));

        private static bool Prefix(NetDataReader reader, int length, IConnectedPlayer data, ref NetworkPacketSerializer<MultiplayerSessionManager.MessageType, IConnectedPlayer> __instance)
        {
            Plugin.Logger.Debug("Deserialize Packet");
            byte @byte = reader.GetByte();
            length--;
            Action<NetDataReader, int, IConnectedPlayer> action;
            if (__instance._messsageHandlers.TryGetValue(@byte, out action))
            {
                if (action != null)
                {
                    Type? packetType = __instance._typeRegistry.FirstOrDefault(x => x.Value == @byte).Key;

                    Plugin.Logger.Debug($"Found MessageHandler for Packet identifier {@byte} {(MultiplayerSessionManager.MessageType)@byte} {packetType}");
                    action(reader, length, data);
                    return false;
                }
            }
            else
            {
                Plugin.Logger.Error($"Received unknown packet type {@byte} from player '{data?.userName ?? "<NULL>"}|{data?.userId ?? " < NULL > "}'");
                reader.SkipBytes(length);
            }
            return false;
        }
    }

    [HarmonyPatch]
    internal class DebugPatchesMenuRpcManagerPacketProcessor
    {
        static MethodBase TargetMethod() => AccessTools.Method(typeof(NetworkPacketSerializer<MenuRpcManager.RpcType, IConnectedPlayer>), nameof(NetworkPacketSerializer<MenuRpcManager.RpcType, IConnectedPlayer>.ProcessPacketInternal));

        private static bool Prefix(NetDataReader reader, int length, IConnectedPlayer data, ref NetworkPacketSerializer<MenuRpcManager.RpcType, IConnectedPlayer> __instance)
        {
            Plugin.Logger.Debug("Deserialize Packet");
            byte @byte = reader.GetByte();
            length--;
            Action<NetDataReader, int, IConnectedPlayer> action;
            if (__instance._messsageHandlers.TryGetValue(@byte, out action))
            {
                if (action != null)
                {
                    Type? packetType = __instance._typeRegistry.FirstOrDefault(x => x.Value == @byte).Key;

                    Plugin.Logger.Debug($"Found MessageHandler for Packet identifier {@byte} {(MenuRpcManager.RpcType)@byte} {packetType}");
                    action(reader, length, data);
                    return false;
                }
            }
            else
            {
                Plugin.Logger.Error($"Received unknown packet type {@byte} from player '{data?.userName ?? "<NULL>"}|{data?.userId ?? " < NULL > "}'");
                reader.SkipBytes(length);
            }
            return false;
        }
    }



    //[HarmonyPatch]
    //internal class DebugPatchesPacketReceiver
    //{
    //    static MethodBase TargetMethod() => AccessTools.Method(typeof(NetworkPacketSerializer<ConnectedPlayerManager.InternalMessageType, IConnectedPlayer>), nameof(NetworkPacketSerializer<ConnectedPlayerManager.InternalMessageType, IConnectedPlayer>.ProcessPacketInternal));

    //    private static bool Prefix(NetDataReader reader, int length, IConnectedPlayer data, ref NetworkPacketSerializer<ConnectedPlayerManager.InternalMessageType, IConnectedPlayer> __instance)
    //    {
    //        Plugin.Logger.Debug("Deserialize Packet");
    //        byte @byte = reader.GetByte();
    //        length--;
    //        Action<NetDataReader, int, IConnectedPlayer> action;
    //        if (__instance._messsageHandlers.TryGetValue(@byte, out action))
    //        {
    //            if (action != null)
    //            {
    //                action(reader, length, data);
    //                return false;
    //            }
    //        }
    //        else
    //        {
    //            Plugin.Logger.Error($"Received unknown packet type {@byte} from player '{data?.userName ?? "<NULL>"}|{data?.userId ?? " < NULL > "}'");
    //            reader.SkipBytes(length);
    //        }
    //        return false;
    //    }
    //}
#endif

}
