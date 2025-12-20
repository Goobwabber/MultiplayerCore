using BGNet.Core.GameLift;
using IPA.Utilities;
using MultiplayerCore.Patchers;
using SiraUtil.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace MultiplayerCore.Objects
{
	public class CustomPlayerSessionProvider : GameLiftPlayerSessionProvider, IGameLiftPlayerSessionProvider
	{
		internal NetworkConfigPatcher? _customNetconfig;
		private readonly SiraLog _logger;
		public CustomPlayerSessionProvider(INetworkConfig networkConfig, IMultiplayerStatusModel multiplayerStatusModel/*, NetworkConfigPatcher customNetconfig*/, SiraLog logger) : base(networkConfig, multiplayerStatusModel)
		{
			//_customNetconfig = customNetconfig;
			_logger = logger;
		}

		public new async Task<PlayerSessionInfo> GetGameLiftPlayerSessionInfo(IAuthenticationTokenProvider authenticationTokenProvider, string userId, BeatmapLevelSelectionMask beatmapLevelSelectionMask, GameplayServerConfiguration gameplayServerConfiguration, string secret, string code, CancellationToken cancellationToken)
		{
			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();
			string ticketId = null;
			string placementId = null;
			string graphAPIAccessToken = _networkConfig.graphAccessToken;
			AuthenticationToken.PlatformType? userPlatform = null;
			AuthenticationToken? singleUsePlatformAuthToken = null;
			XPlatformAccessTokenData xPlatformAccessToken = await GetXPlatformAccessToken(authenticationTokenProvider, cancellationToken);
			if (xPlatformAccessToken.IsValid())
			{
				userPlatform = authenticationTokenProvider.GetTokenPlatform(xPlatformAccessToken.platformEnvironment);
				graphAPIAccessToken = xPlatformAccessToken.token;
			}

			try
			{
				int numAttempts = 0;
				while (stopwatch.ElapsedMilliseconds < 120000 && !cancellationToken.IsCancellationRequested)
				{
					if (!xPlatformAccessToken.IsValid() || (_customNetconfig != null && _customNetconfig.IsOverridingApi))
					{
						singleUsePlatformAuthToken = await authenticationTokenProvider.GetAuthenticationToken();
						userPlatform = singleUsePlatformAuthToken.Value.platform;
					}

					GetMultiplayerInstanceResponse getMatchmakingInstanceResponse;
					try
					{
						numAttempts++;
						getMatchmakingInstanceResponse = await _graphAPIClient.Post<GetMultiplayerInstanceRequest, GetMultiplayerInstanceResponse>("beat_saber_get_multiplayer_instance", graphAPIAccessToken, new GetMultiplayerInstanceRequest(Application.version, _networkConfig.serviceEnvironment, userId, beatmapLevelSelectionMask, gameplayServerConfiguration, userPlatform.Value, singleUsePlatformAuthToken?.userId, singleUsePlatformAuthToken?.sessionToken, secret, code, GetAverageLatencies(), ticketId, placementId, _networkConfig.customLocation), default(GraphAPIClient.PostOptions), cancellationToken);
					}
					catch (ConnectionFailedException ex) when (numAttempts <= 1 && ex.reason == ConnectionFailedReason.AuthenticationFailed)
					{
						xPlatformAccessToken = await GetXPlatformAccessToken(authenticationTokenProvider, cancellationToken, skipCache: true);
						if (xPlatformAccessToken.IsValid())
						{
							userPlatform = authenticationTokenProvider.GetTokenPlatform(xPlatformAccessToken.platformEnvironment);
							graphAPIAccessToken = xPlatformAccessToken.token;
						}

						continue;
					}

					if (getMatchmakingInstanceResponse.errorCode != 0 && getMatchmakingInstanceResponse.errorCode != MultiplayerPlacementErrorCode.RequestTimeout)
					{
						throw new ConnectionFailedException(getMatchmakingInstanceResponse.errorCode.ToConnectionFailedReason());
					}

					if (getMatchmakingInstanceResponse.playerSessionInfo != null && !string.IsNullOrEmpty(getMatchmakingInstanceResponse.playerSessionInfo.gameSessionId) && !string.IsNullOrEmpty(getMatchmakingInstanceResponse.playerSessionInfo.playerSessionId))
					{
						return getMatchmakingInstanceResponse.playerSessionInfo;
					}

					if (string.IsNullOrEmpty(getMatchmakingInstanceResponse.ticketId) && string.IsNullOrEmpty(getMatchmakingInstanceResponse.placementId))
					{
						throw new ConnectionFailedException(ConnectionFailedReason.Timeout);
					}

					ticketId = getMatchmakingInstanceResponse.ticketId;
					placementId = getMatchmakingInstanceResponse.placementId;
					if (getMatchmakingInstanceResponse.pollIntervalMs > 0)
					{
						await Task.Delay(getMatchmakingInstanceResponse.pollIntervalMs, cancellationToken);
					}
				}
			}
			catch (TaskCanceledException)
			{
			}

			if (!cancellationToken.IsCancellationRequested)
			{
				throw new ConnectionFailedException(ConnectionFailedReason.FailedToFindMatch);
			}

			if (!string.IsNullOrEmpty(ticketId) || !string.IsNullOrEmpty(placementId))
			{
				if (!xPlatformAccessToken.IsValid())
				{
					singleUsePlatformAuthToken = await authenticationTokenProvider.GetAuthenticationToken();
					userPlatform = singleUsePlatformAuthToken.Value.platform;
				}

				try
				{
					CancellationToken token = new CancellationTokenSource(TimeSpan.FromSeconds(5.0)).Token;
					await _graphAPIClient.Post<GetMultiplayerInstanceRequest, GetMultiplayerInstanceResponse>("beat_saber_multiplayer_cancel_matchmaking_ticket", graphAPIAccessToken, new GetMultiplayerInstanceRequest(Application.version, _networkConfig.serviceEnvironment, userId, beatmapLevelSelectionMask, gameplayServerConfiguration, userPlatform.Value, singleUsePlatformAuthToken?.userId, singleUsePlatformAuthToken?.sessionToken, secret, code, GetAverageLatencies(), ticketId, placementId), default(GraphAPIClient.PostOptions), token);
				}
				catch
				{
				}
			}

			throw new TaskCanceledException("Cancelled Request");
		}


		internal void UpdateServer()
		{
			GameLiftPlayerSessionProvider instance = this as GameLiftPlayerSessionProvider;
			instance.SetField(nameof(_graphAPIClient), new GraphAPIClient(_networkConfig.graphUrl, _networkConfig.graphAccessToken));
			GetMultiplayerStatusData();
		}


	}
}
