using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using HMUI;
using IPA.Utilities.Async;
using MultiplayerCore.Beatmaps.Providers;
using MultiplayerCore.Networking;
using MultiplayerCore.Players.Packets;
using SiraUtil.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zenject;

namespace MultiplayerCore.UI
{
	internal class MpPerPlayerUI : IInitializable, IDisposable
	{
		public static string DifficultySelectorResourcePath => "MultiplayerCore.UI.MpDifficultySelector.bsml";
		public static string PerPlayerTogglesResourcePath => "MultiplayerCore.UI.MpPerPlayerToggles.bsml";
		private readonly GameServerLobbyFlowCoordinator _gameServerLobbyFlowCoordinator;
		private readonly LobbySetupViewController _lobbyViewController;
		private readonly IMultiplayerSessionManager _multiplayerSessionManager;
		private readonly ILobbyGameStateController _gameStateController;
		private readonly BeatmapLevelsModel _beatmapLevelsModel;
		private readonly MpPacketSerializer _packetSerializer;
		private readonly MpBeatmapLevelProvider _beatmapLevelProvider;
		private BeatmapKey _currentBeatmapKey;
		private List<string>? _allowedDiffs;
		private CanvasGroup? _difficultyCanvasGroup;

		private readonly SiraLog _logger;

		public MpPerPlayerUI(
			GameServerLobbyFlowCoordinator gameServerLobbyFlowCoordinator, 
			BeatmapLevelsModel beatmapLevelsModel, 
			IMultiplayerSessionManager sessionManager, 
			MpBeatmapLevelProvider beatmapLevelProvider, 
			MpPacketSerializer packetSerializer,
			SiraLog logger)
		{
			_gameServerLobbyFlowCoordinator = gameServerLobbyFlowCoordinator;
			_lobbyViewController = _gameServerLobbyFlowCoordinator._lobbySetupViewController;
			_gameStateController = _gameServerLobbyFlowCoordinator._lobbyGameStateController;
			_beatmapLevelsModel = beatmapLevelsModel;
			_multiplayerSessionManager = sessionManager;
			_beatmapLevelProvider = beatmapLevelProvider;
			_packetSerializer = packetSerializer;
			_logger = logger;
		}

		// Ignore never assigned warning
#pragma warning disable 0649 // Field is never assigned to, and will always have its default value
#pragma warning disable IDE0044 // Add modifier "readonly"

		[UIComponent("segmentVert")]
		private VerticalLayoutGroup? segmentVert;

		[UIComponent("difficultyControl")]

		private TextSegmentedControl? difficultyControl;

		[UIComponent("ppdt")]
		private ToggleSetting? ppdt;

		[UIComponent("ppmt")]
		private ToggleSetting? ppmt;
#pragma warning restore IDE0044 // Add modifier "readonly"
#pragma warning restore 0649 // Field is never assigned to, and will always have its default value

		public void Initialize()
		{
			// DifficultySelector
			BSMLParser.instance.Parse(
				BeatSaberMarkupLanguage.Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), 
					DifficultySelectorResourcePath), _lobbyViewController._beatmapSelectionView.gameObject, this);
			_difficultyCanvasGroup = segmentVert?.gameObject.AddComponent<CanvasGroup>();

			// PerPlayerToggle
			BSMLParser.instance.Parse(
				BeatSaberMarkupLanguage.Utilities.GetResourceContent(Assembly.GetExecutingAssembly(),
					PerPlayerTogglesResourcePath), _lobbyViewController._startGameReadyButton.gameObject, this);

			// Check UI Elements
			if (difficultyControl == null || ppmt == null || ppdt == null || segmentVert == null || _difficultyCanvasGroup == null)
			{
				_logger.Critical("Error could not initialize UI");
				return;
			}

			_lobbyViewController.didActivateEvent += DidActivate;
			_lobbyViewController.didDeactivateEvent += DidDeactivate;

			_packetSerializer.RegisterCallback<MpPerPlayerPacket>(HandleMpPerPlayerPacket);
			_packetSerializer.RegisterCallback<GetMpPerPlayerPacket>(HandleGetMpPerPlayerPacket);

			// We register the callbacks
			_gameStateController.lobbyStateChangedEvent += SetLobbyState;
			_gameServerLobbyFlowCoordinator._multiplayerLevelSelectionFlowCoordinator.didSelectLevelEvent += LocalSelectedBeatmap;
			_gameServerLobbyFlowCoordinator._serverPlayerListViewController.selectSuggestedBeatmapEvent += UpdateDifficultyListWithBeatmapKey;
			_lobbyViewController.clearSuggestedBeatmapEvent += ClearLocalSelectedBeatmap;
			_gameServerLobbyFlowCoordinator._lobbyPlayerPermissionsModel.permissionsChangedEvent += UpdateButtonsEnabled;
		}

		public void Dispose()
		{
			_lobbyViewController.didActivateEvent -= DidActivate;
			_lobbyViewController.didDeactivateEvent -= DidDeactivate;

			_packetSerializer.UnregisterCallback<MpPerPlayerPacket>();
			_packetSerializer.UnregisterCallback<GetMpPerPlayerPacket>();
		}

		public void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
		{
			if (firstActivation)
			{
				var cgubPos = _lobbyViewController._cancelGameUnreadyButton.transform.position;
				cgubPos.y -= 0.2f;
				_lobbyViewController._cancelGameUnreadyButton.transform.position = cgubPos;

				var sgrbPos = _lobbyViewController._startGameReadyButton.transform.position;
				sgrbPos.y -= 0.2f;
				_lobbyViewController._startGameReadyButton.transform.position = sgrbPos;

				var csgHHPos = _lobbyViewController._cantStartGameHoverHint.transform.position;
				csgHHPos.y -= 0.2f;
				_lobbyViewController._cantStartGameHoverHint.transform.position = csgHHPos;
			}
			else if (!firstActivation)
			{
				var lobbyPlayersDataModel = _gameServerLobbyFlowCoordinator._lobbyPlayersDataModel as LobbyPlayersDataModel;
				LobbyPlayerData? playerData =
					lobbyPlayersDataModel?.GetOrCreateLobbyPlayerDataModel(lobbyPlayersDataModel.localUserId,
						out _);
				if (playerData != null)
					UpdateDifficultyListWithBeatmapKey(playerData.beatmapKey);

				if (addedToHierarchy)
				{
					// Reset our buttons
					ppdt.Value = false;
					ppmt.Value = false;

					// Request Updated state
					_multiplayerSessionManager.Send(new GetMpPerPlayerPacket());
				}
			}


			if (_lobbyViewController._isPartyOwner)
			{
				ppdt?.gameObject.SetActive(true);
				ppmt?.gameObject.SetActive(true);
			}
			else
			{
				ppdt?.gameObject.SetActive(false);
				ppmt?.gameObject.SetActive(false);
			}
		}

		public void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
		{
			if (removedFromHierarchy)
			{

			}
		}

		#region DiffListUpdater
		public string DiffToStr(BeatmapDifficulty difficulty)
		{
			return difficulty == BeatmapDifficulty.ExpertPlus ? "Expert+" : difficulty.ToString();
		}

		private void UpdateDifficultyListWithBeatmapKey(BeatmapKey beatmapKey)
		{
			_currentBeatmapKey = beatmapKey;
			if (!_currentBeatmapKey.IsValid())
			{
				_logger.Debug("Selected BeatmapKey invalid, disabling difficulty selector");
				segmentVert?.gameObject.SetActive(false);
				return;
			}
			var levelHash = Utilities.HashForLevelID(beatmapKey.levelId);
			if (levelHash != null)
			{
				_logger.Debug($"Level is custom, trying to get beatmap for hash {levelHash}");
				_beatmapLevelProvider.GetBeatmap(levelHash).ContinueWith(levelTask =>
				{
					if (levelTask.IsCompleted && !levelTask.IsFaulted && levelTask.Result != null)
					{
						var level = levelTask.Result;
						_logger.Debug($"Got level {level.LevelHash}, {level.Requirements}, {level.Requirements[beatmapKey.beatmapCharacteristic.serializedName]}");
						// Hacky we use requirements to get the available difficulties
						UpdateDifficultyList(level.Requirements[beatmapKey.beatmapCharacteristic.serializedName].Keys
							.ToList());
					}
					else _logger.Error($"Failed to get level for hash {levelHash}");
				});
			}
			else
			{
				_logger.Debug($"LevelId not custom: {beatmapKey.levelId}, getting difficulties from basegame");
				var diffList = _beatmapLevelsModel.GetBeatmapLevel(beatmapKey.levelId)
					?.GetDifficulties(beatmapKey.beatmapCharacteristic).ToList();
				if (diffList != null) UpdateDifficultyList(diffList);
			}
		}

		private void UpdateDifficultyList(IReadOnlyList<BeatmapDifficulty> difficulties)
		{
			// Ensure that we run on the UnityMainThread here
			UnityMainThreadTaskScheduler.Factory.StartNew(() =>
			{
				_allowedDiffs = (from diff in difficulties
								 where _gameServerLobbyFlowCoordinator._unifiedNetworkPlayerModel.selectionMask.difficulties
									 .Contains(diff)
								 select DiffToStr(diff)
					).ToList();
				foreach (var difficultyStr in _allowedDiffs)
					_logger.Debug($"Allowed difficulty='{difficultyStr}'");

				if (_allowedDiffs.Count > 1)
				{
					segmentVert?.gameObject.SetActive(true);
					difficultyControl?.SetTexts(_allowedDiffs);
					int index = _allowedDiffs.IndexOf(DiffToStr(_currentBeatmapKey.difficulty));
					if (index > 0)
						difficultyControl?.SelectCellWithNumber(index);
				}
				else
				{
					_logger.Debug("Less than 2 Difficulties available disabling selector");
					segmentVert?.gameObject.SetActive(false);
				}
			}
			);
		}
		#endregion

		#region Callbacks
		private void HandleMpPerPlayerPacket(MpPerPlayerPacket packet, IConnectedPlayer player)
		{
			_logger.Debug($"Received MpPerPlayerPacket from {player.userName}|{player.userId} with values PPDEnabled={packet.PPDEnabled}, PPMEnabled={packet.PPMEnabled}");
			if ((packet.PPDEnabled != PerPlayerDifficulty || packet.PPMEnabled != PerPlayerModifiers) && 
			    ppdt != null && ppmt != null && player.isConnectionOwner)
			{
				ppdt.Value = packet.PPDEnabled;
				ppmt.Value = packet.PPMEnabled;
			}
			else if (!player.isConnectionOwner)
			{
				_logger.Warn("Player is not Connection Owner, ignoring packet");
			}
		}

		private void HandleGetMpPerPlayerPacket(GetMpPerPlayerPacket packet, IConnectedPlayer player)
		{
			_logger.Debug($"Received GetMpPerPlayerPacket from {player.userName}|{player.userId}");
			// Send MpPerPlayerPacket
			var ppPacket = new MpPerPlayerPacket();
			ppPacket.PPDEnabled = ppdt.Value;
			ppPacket.PPMEnabled = ppmt.Value;
			_multiplayerSessionManager.Send(ppPacket);
		}

		private void UpdateButtonsEnabled()
		{
			if (_lobbyViewController._isPartyOwner)
			{
				ppdt?.gameObject.SetActive(true);
				ppmt?.gameObject.SetActive(true);

				// Request updated button states from server
				_multiplayerSessionManager.Send(new GetMpPerPlayerPacket());
			}
			else
			{
				ppdt?.gameObject.SetActive(false);
				ppmt?.gameObject.SetActive(false);
			}
		}

		private void SetLobbyState(MultiplayerLobbyState lobbyState)
		{
			_logger.Debug($"Current Lobby State {lobbyState}");
			if (_difficultyCanvasGroup != null)
				_difficultyCanvasGroup.alpha = (lobbyState == MultiplayerLobbyState.LobbySetup ||
												lobbyState == MultiplayerLobbyState.LobbyCountdown) ? 1f : 0.25f;

			foreach (var cell in difficultyControl.cells)
			{
				cell.interactable = lobbyState == MultiplayerLobbyState.LobbySetup ||
				                    lobbyState == MultiplayerLobbyState.LobbyCountdown;
			}

			if (_lobbyViewController == null)
				return;

			var raycaster = difficultyControl?.GetComponent<BaseRaycaster>();

			if (_lobbyViewController._isPartyOwner)
			{
				ppdt.interactable = lobbyState == MultiplayerLobbyState.LobbySetup ||
				                                     lobbyState == MultiplayerLobbyState.LobbyCountdown;
				ppmt.interactable = lobbyState == MultiplayerLobbyState.LobbySetup || 
				                                         lobbyState == MultiplayerLobbyState.LobbyCountdown;
			}
		}

		private void LocalSelectedBeatmap(LevelSelectionFlowCoordinator.State state)
		{
			_currentBeatmapKey = state.beatmapKey;
			UpdateDifficultyList(state.beatmapLevel.GetDifficulties(state.beatmapKey.beatmapCharacteristic).ToList());
		}

		private void ClearLocalSelectedBeatmap()
		{
			segmentVert?.gameObject.SetActive(false);
			_currentBeatmapKey = new BeatmapKey();
		}

		#endregion
		#region UIValues

		[UIValue("PerPlayerDifficulty")]
		public bool PerPlayerDifficulty
		{
			get => ppdt.Value;
			set
			{
				ppdt.Value = value;
				_multiplayerSessionManager.Send(new MpPerPlayerPacket
				{
					PPDEnabled = ppdt.Value,
					PPMEnabled = ppmt.Value
				});
			}
		}

		[UIValue("PerPlayerModifiers")]
		public bool PerPlayerModifiers
		{
			get => ppmt.Value;
			set
			{
				ppmt.Value = value;
				_multiplayerSessionManager.Send(new MpPerPlayerPacket
				{
					PPDEnabled = ppdt.Value,
					PPMEnabled = ppmt.Value
				});
			}
		}

		[UIAction("SetSelectedDifficulty")]
		public void SetSelectedDifficulty(TextSegmentedControl _, int index)
		{
			var diff = _allowedDiffs[index];
			_logger.Debug($"Selected difficulty at {index} - {diff}");
			if (Enum.TryParse(diff.Replace("Expert+", "ExpertPlus"), out BeatmapDifficulty difficulty))
			{
				_currentBeatmapKey = new BeatmapKey(_currentBeatmapKey.levelId,
					_currentBeatmapKey.beatmapCharacteristic, difficulty);
				_gameServerLobbyFlowCoordinator._lobbyPlayersDataModel.SetLocalPlayerBeatmapLevel(_currentBeatmapKey);
			}
		}
		#endregion

	}
}
