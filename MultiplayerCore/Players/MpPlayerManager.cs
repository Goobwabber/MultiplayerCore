using MultiplayerCore.Networking;
using OculusStudios.Platform.Core;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Zenject;

namespace MultiplayerCore.Players
{
    public class MpPlayerManager : IInitializable, IDisposable
    {
        public event Action<IConnectedPlayer, MpPlayerData> PlayerConnectedEvent = null!;

        public IReadOnlyDictionary<string, MpPlayerData> Players => _playerData;

        private UserInfo _localPlayerInfo = null!;
        private ConcurrentDictionary<string, MpPlayerData> _playerData = new();

        private readonly MpPacketSerializer _packetSerializer;
        private readonly IBeatSaberMultiplayerSessionManager _sessionManager;
        private readonly IPlatform _platform;

        internal MpPlayerManager(
            MpPacketSerializer packetSerializer,
			IBeatSaberMultiplayerSessionManager sessionManager,
            IPlatform platform)
        {
            _packetSerializer = packetSerializer;
            _sessionManager = sessionManager;
            _platform = platform;
        }

        public void Initialize()
        {
            _sessionManager.SetLocalPlayerState("modded", true);
            _packetSerializer.RegisterCallback<MpPlayerData>(HandlePlayerData);
			//_sessionManager.playerConnectedEvent += HandlePlayerConnected;

            InitUserInfo();
        }

        public void InitUserInfo()
        {
			string key = _platform.key;
			UserInfo.Platform platform;
			if (!(key == "steam"))
			{
				if (!(key == "oculus"))
				{
					if (!(key == "oculus-mock"))
					{
						if (!(key == "mock"))
						{
							throw new NotImplementedException();
						}
						platform = UserInfo.Platform.Test;
					}
					else
					{
						platform = UserInfo.Platform.Oculus;
					}
				}
				else
				{
					platform = UserInfo.Platform.Oculus;
				}
			}
			else
			{
				platform = UserInfo.Platform.Steam;
			}
			_localPlayerInfo = new UserInfo(platform, _platform.user.userId.ToString(), _platform.user.displayName);
		}

		public void Dispose()
        {
            _packetSerializer.UnregisterCallback<MpPlayerData>();
        }

        //private void HandlePlayerConnected(IConnectedPlayer player)
        //{
	       // if (_localPlayerInfo == null) 
		      //  throw new NullReferenceException("local player info was not yet set! make sure it is set before anything else happens!");

        //    _sessionManager.Send(new MpPlayerData
        //    {
        //        Platform = _localPlayerInfo.platform switch
        //        {
        //            UserInfo.Platform.Oculus => Platform.OculusPC,
        //            UserInfo.Platform.Steam => Platform.Steam,
        //            _ => Platform.Unknown
        //        },
        //        PlatformId = _localPlayerInfo.platformUserId
        //    });
        //}

        private void HandlePlayerData(MpPlayerData packet, IConnectedPlayer player)
        {
            _playerData[player.userId] = packet;
        }

        public bool TryGetPlayer(string userId, out MpPlayerData player)
            => _playerData.TryGetValue(userId, out player);

        public MpPlayerData? GetPlayer(string userId)
            => _playerData.ContainsKey(userId) ? _playerData[userId] : null;
    }
}
