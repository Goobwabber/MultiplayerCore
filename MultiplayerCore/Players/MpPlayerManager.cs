using LiteNetLib.Utils;
using MultiplayerCore.Networking;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Zenject;

namespace MultiplayerCore.Players
{
    public class MpPlayerManager : IInitializable, IDisposable
    {
		public IReadOnlyDictionary<string, MpPlayer> Players => _playerData;

		private UserInfo _localPlayerInfo = null!;
		private ConcurrentDictionary<string, MpPlayer> _playerData = new();

		private readonly MpPacketSerializer _packetSerializer;
		private readonly IPlatformUserModel _platformUserModel;
		private readonly IMultiplayerSessionManager _sessionManager;

		internal MpPlayerManager(
			MpPacketSerializer packetSerializer,
			IPlatformUserModel platformUserModel,
			IMultiplayerSessionManager sessionManager)
		{
			_packetSerializer = packetSerializer;
			_platformUserModel = platformUserModel;
			_sessionManager = sessionManager;
		}

		public async void Initialize()
		{
			_sessionManager.SetLocalPlayerState("modded", true);
			_packetSerializer.RegisterCallback<MpPlayer>(HandlePlayerData);
			_localPlayerInfo = await _platformUserModel.GetUserInfo();
            _sessionManager.playerConnectedEvent += HandlePlayerConnected;
		}

        public void Dispose()
        {
			_packetSerializer.UnregisterCallback<MpPlayer>();
        }

		private void HandlePlayerConnected(IConnectedPlayer player)
		{
			_sessionManager.Send(new MpPlayer
			{
				PlatformId = _localPlayerInfo.platformUserId,
				Platform = _localPlayerInfo.platform switch
                {
                    UserInfo.Platform.Test => Platform.Unknown,
                    UserInfo.Platform.Steam => Platform.Steam,
                    UserInfo.Platform.Oculus => Platform.OculusPC,
                    UserInfo.Platform.PS4 => Platform.PS4,
                    _ => throw new NotImplementedException()
                }
            });
		}

		private void HandlePlayerData(MpPlayer packet, IConnectedPlayer player)
        {
			_playerData[player.userId] = packet;
        }

		public bool TryGetPlayer(string userId, out MpPlayer player)
			=> _playerData.TryGetValue(userId, out player);
	}
}
