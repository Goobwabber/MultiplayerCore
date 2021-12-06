using LiteNetLib.Utils;
using SiraUtil.Logging;
using System;
using System.Collections.Generic;
using Zenject;

namespace MultiplayerCore.Networking
{
    public class MpPacketSerializer : INetworkPacketSubSerializer<IConnectedPlayer>, IInitializable, IDisposable
    {
        private const int ID = 100;

        private Dictionary<string, Action<NetDataReader, int, IConnectedPlayer>> packetHandlers = new();
        private List<Type> registeredTypes = new();

        private readonly MultiplayerSessionManager _sessionManager;
        private readonly SiraLog _logger;

        internal MpPacketSerializer(
            IMultiplayerSessionManager sessionManager, 
            SiraLog logger)
        {
            _sessionManager = (sessionManager as MultiplayerSessionManager)!;
            _logger = logger;
        }

        public void Initialize()
            => _sessionManager.RegisterSerializer((MultiplayerSessionManager.MessageType)ID, this);

        public void Dispose()
            => _sessionManager.UnregisterSerializer((MultiplayerSessionManager.MessageType)ID, this);

        /// <summary>
        /// Method the base game uses to serialize an <see cref="INetSerializable"/>.
        /// </summary>
        /// <param name="writer">The buffer to write to</param>
        /// <param name="packet">The packet to serialize</param>
        public void Serialize(NetDataWriter writer, INetSerializable packet)
        {
            writer.Put(packet.GetType().Name);
            packet.Serialize(writer);
        }

        /// <summary>
        /// Method the base game uses to deserialize (handle) a packet.
        /// </summary>
        /// <param name="reader">The buffer to read from</param>
        /// <param name="length">Length of the packet</param>
        /// <param name="data">The sender of the packet</param>
        public void Deserialize(NetDataReader reader, int length, IConnectedPlayer data)
        {
            int prevPosition = reader.Position;
            string packetType = reader.GetString();
            length -= reader.Position - prevPosition;
            prevPosition = reader.Position;

            Action<NetDataReader, int, IConnectedPlayer> action;
            if (packetHandlers.TryGetValue(packetType, out action) && action != null)
            {
                try
                {
                    action(reader, length, data);
                }
                catch (Exception ex)
                {
                    _logger.Warn($"An exception was thrown processing custom packet '{packetType}' from player '{data?.userName ?? "<NULL>"}|{data?.userId ?? " < NULL > "}': {ex.Message}");
                    _logger.Debug(ex);
                }
            }

            // skip any unprocessed bytes (or rewind the reader if too many bytes were read)
            int processedBytes = reader.Position - prevPosition;
            reader.SkipBytes(length - processedBytes);
        }

        /// <summary>
        /// Method the base game uses to see if this serializer can handle a type.
        /// </summary>
        /// <param name="type">The type to be handled</param>
        /// <returns>Whether this serializer can handle the type</returns>
        public bool HandlesType(Type type)
        {
            return registeredTypes.Contains(type);
        }

        /// <summary>
        /// Registers a callback without sender for a packet.
        /// </summary>
        /// <typeparam name="TPacket">Type of packet to register. Inherits <see cref="LiteNetLib.Utils.INetSerializable"/></typeparam>
        /// <param name="callback">Action that handles received packet.</param>
        /// <seealso cref="RegisterCallback{TPacket}(Action{TPacket, IConnectedPlayer})"/>
        public void RegisterCallback<TPacket>(Action<TPacket> callback) where TPacket : INetSerializable, new()
            => RegisterCallback<TPacket>((TPacket packet, IConnectedPlayer player) => callback?.Invoke(packet));

        /// <summary>
        /// Registers a callback including sender for a packet.
        /// </summary>
        /// <typeparam name="TPacket">Type of packet to register. Inherits <see cref="LiteNetLib.Utils.INetSerializable"/></typeparam>
        /// <param name="callback">Action that handles received packet and sender</param>
        /// <seealso cref="RegisterCallback{TPacket}(Action{TPacket})"/>
        public void RegisterCallback<TPacket>(Action<TPacket, IConnectedPlayer> callback) where TPacket : INetSerializable, new()
        {
            registeredTypes.Add(typeof(TPacket));

            Func<NetDataReader, int, TPacket> deserialize = delegate (NetDataReader reader, int size)
            {
                TPacket packet = new TPacket();
                if (packet == null)
                {
                    _logger.Error($"Constructor for '{typeof(TPacket)}' returned null!");
                    reader.SkipBytes(size);
                }
                else
                {
                    packet.Deserialize(reader);
                }

                return packet!;
            };

            packetHandlers[typeof(TPacket).Name] = delegate (NetDataReader reader, int size, IConnectedPlayer player)
            {
                callback(deserialize(reader, size), player);
            };

            _logger.Debug($"Registered packet '{typeof(TPacket).Name}'.");
        }

        /// <summary>
        /// Unregisters a callback for a packet.
        /// </summary>
        /// <typeparam name="TPacket">Type of packet to unregister. Inherits <see cref="LiteNetLib.Utils.INetSerializable"/></typeparam>
        public void UnregisterCallback<TPacket>() where TPacket : INetSerializable, new()
            => packetHandlers.Remove(typeof(TPacket).ToString());
    }
}
