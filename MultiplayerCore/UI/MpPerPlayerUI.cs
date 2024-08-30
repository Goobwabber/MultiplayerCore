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
using MultiplayerCore.Models;
using MultiplayerCore.Objects;
using MultiplayerCore.Repositories;
using UnityEngine;
using UnityEngine.UI;
using Zenject;
using MultiplayerCore.Patchers;

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
		private readonly MpPlayersDataModel _playersDataModel;
		private readonly MpStatusRepository _statusRepository;
		private readonly NetworkConfigPatcher _networkConfig;
		private BeatmapKey _currentBeatmapKey;
		private List<string>? _allowedDiffs;
		private CanvasGroup? _difficultyCanvasGroup;
		private MpStatusData? _currentStatusData;

		private readonly SiraLog _logger;

		public MpPerPlayerUI(
			GameServerLobbyFlowCoordinator gameServerLobbyFlowCoordinator, 
			BeatmapLevelsModel beatmapLevelsModel, 
			IMultiplayerSessionManager sessionManager, 
			MpBeatmapLevelProvider beatmapLevelProvider, 
			MpPacketSerializer packetSerializer,
			MpStatusRepository statusRepository,
			NetworkConfigPatcher networkConfig,
			SiraLog logger)
		{
			_gameServerLobbyFlowCoordinator = gameServerLobbyFlowCoordinator;
			_lobbyViewController = _gameServerLobbyFlowCoordinator._lobbySetupViewController;
			_gameStateController = _gameServerLobbyFlowCoordinator._lobbyGameStateController;
			_beatmapLevelsModel = beatmapLevelsModel;
			_multiplayerSessionManager = sessionManager;
			_beatmapLevelProvider = beatmapLevelProvider;
			_playersDataModel = _gameServerLobbyFlowCoordinator._lobbyPlayersDataModel as MpPlayersDataModel;
			_packetSerializer = packetSerializer;
			_statusRepository = statusRepository;
			_networkConfig = networkConfig;
			_logger = logger;
		}

		// Ignore never assigned warning
#pragma warning disable 0649 // Field is never assigned to, and will always have its default value
#pragma warning disable IDE0044 // Add modifier "readonly"

		[UIComponent("segmentVert")]
		private VerticalLayoutGroup? segmentVert;

		[UIComponent("ppth")]
		private HorizontalLayoutGroup? ppth;

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
					PerPlayerTogglesResourcePath), _lobbyViewController.gameObject, this);

			// Check UI Elements
			if (difficultyControl == null || ppmt == null || ppdt == null || segmentVert == null || _difficultyCanvasGroup == null || ppth == null)
			{
				_logger.Critical("Error could not initialize UI");
				return;
			}

			_lobbyViewController.didActivateEvent += DidActivate;
			_gameServerLobbyFlowCoordinator._selectModifiersViewController.didActivateEvent +=
				ModifierSelectionDidActivate;
			//_lobbyViewController.didDeactivateEvent += DidDeactivate;

			_packetSerializer.RegisterCallback<MpPerPlayerPacket>(HandleMpPerPlayerPacket);
			_packetSerializer.RegisterCallback<GetMpPerPlayerPacket>(HandleGetMpPerPlayerPacket);

			// We register the callbacks
			_gameStateController.lobbyStateChangedEvent += SetLobbyState;
			_gameServerLobbyFlowCoordinator._multiplayerLevelSelectionFlowCoordinator.didSelectLevelEvent += LocalSelectedBeatmap;
			_gameServerLobbyFlowCoordinator._serverPlayerListViewController.selectSuggestedBeatmapEvent += UpdateDifficultyListWithBeatmapKey;
			_lobbyViewController.clearSuggestedBeatmapEvent += ClearLocalSelectedBeatmap;
			_gameServerLobbyFlowCoordinator._lobbyPlayerPermissionsModel.permissionsChangedEvent += UpdateButtonsEnabled;

			_statusRepository.statusUpdatedForUrlEvent += HandleStatusUpdate;
		}

		public void Dispose()
		{
			_lobbyViewController.didActivateEvent -= DidActivate;
			_gameServerLobbyFlowCoordinator._selectModifiersViewController.didActivateEvent -=
				ModifierSelectionDidActivate;
			//_lobbyViewController.didDeactivateEvent -= DidDeactivate;

			_packetSerializer.UnregisterCallback<MpPerPlayerPacket>();
			_packetSerializer.UnregisterCallback<GetMpPerPlayerPacket>();

			_statusRepository.statusUpdatedForUrlEvent -= HandleStatusUpdate;
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
					ppdt!.Value = false;
					ppmt!.Value = false;

					// Request Updated state
					_multiplayerSessionManager.Send(new GetMpPerPlayerPacket());
				}
			}

			ppdt!.interactable = _lobbyViewController._isPartyOwner;
			ppdt!.text.alpha = _lobbyViewController._isPartyOwner ? 1f : 0.25f;
			ppmt!.interactable = _lobbyViewController._isPartyOwner;
			ppmt!.text.alpha = _lobbyViewController._isPartyOwner ? 1f : 0.25f;

			// Move toggles to correct position
			var locposition = _lobbyViewController._startGameReadyButton.gameObject.transform.localPosition;
			ppth!.gameObject.transform.localPosition = locposition;

			ppth.gameObject.SetActive(_networkConfig.IsOverridingApi && (_currentStatusData.supportsPPDifficulties || _currentStatusData.supportsPPModifiers));
		}

		void ModifierSelectionDidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
		{
			_logger.Trace("ModifierSelectionDidActivate");
			var modifierController = _gameServerLobbyFlowCoordinator._selectModifiersViewController
				._gameplayModifiersPanelController;
			//modifierController._gameplayModifiers =
			//	modifierController.gameplayModifiers.CopyWith(songSpeed: GameplayModifiers.SongSpeed.Normal);
			var toggles = modifierController._gameplayModifierToggles;
			foreach (var toggle in toggles)
			{
				_logger.Trace("Toggle: " + toggle.gameObject.name);
				if (toggle.gameObject.name == "FasterSong" || toggle.gameObject.name == "SuperFastSong" ||
				    toggle.gameObject.name == "SlowerSong")
				{
					toggle.toggle.interactable = !ppmt.Value;
					var canvas = toggle.gameObject.GetComponent<CanvasGroup>();
					if (canvas == null) canvas = toggle.gameObject.AddComponent<CanvasGroup>();
					canvas.alpha = ppmt.Value ? 0.25f : 1f;
				}
			}
		}

		//public void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
		//{
		//	if (removedFromHierarchy)
		//	{

		//	}
		//}

		void HandleStatusUpdate(string statusUrl, MpStatusData statusData)
		{
			_logger.Info($"Got StatusData update for server {statusData.name} with values " +
			             $"supportsPPDifficulties={statusData.supportsPPDifficulties} and " +
			             $"supportsPPModifiers={statusData.supportsPPModifiers}");
			ppth?.gameObject.SetActive(statusData.supportsPPDifficulties || statusData.supportsPPModifiers);
			ppdt?.gameObject.SetActive(statusData.supportsPPDifficulties);
			ppmt?.gameObject.SetActive(statusData.supportsPPModifiers);
			_currentStatusData = statusData;
		}

		#region DiffListUpdater
		// TODO: Possibly replace with BeatmapDifficultyMethods.Name ext method see BeatmapDifficultySegmentedControlController
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
						if (level.Requirements[beatmapKey.beatmapCharacteristic.serializedName].Count > 0)
							UpdateDifficultyList(level.Requirements[beatmapKey.beatmapCharacteristic.serializedName].Keys
								.ToList());
						else
						{
							_logger.Debug(
								$"Level {levelHash} has empty requirements, this should not happen, falling back to packet");
							//level = _beatmapLevelProvider.TryGetBeatmapFromPacketHash(levelHash);
							var packet = _playersDataModel.FindLevelPacket(levelHash);
							level = packet != null ? _beatmapLevelProvider.GetBeatmapFromPacket(packet) : null;
							if (level != null && level.Requirements[beatmapKey.beatmapCharacteristic.serializedName].Count > 0)
								UpdateDifficultyList(level.Requirements[beatmapKey.beatmapCharacteristic.serializedName].Keys
									.ToList());
							else
							{
								_logger.Debug($"Level packet {levelHash} also has empty requirements, this should not happen...");
								UpdateDifficultyList(new [] {beatmapKey.difficulty});
							}
						}
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

			if (player.isConnectionOwner)
			{
				// If a SongSpeed modifier was already set, remove it and re-announce our modifiers
				var modifierController = _gameServerLobbyFlowCoordinator._selectModifiersViewController
					._gameplayModifiersPanelController;
				if (modifierController != null && modifierController.gameplayModifiers != null &&
				    modifierController.gameplayModifiers.songSpeed != GameplayModifiers.SongSpeed.Normal)
				{
					modifierController._gameplayModifiers =
						modifierController.gameplayModifiers.CopyWith(songSpeed: GameplayModifiers.SongSpeed.Normal);
					_gameServerLobbyFlowCoordinator._lobbyPlayersDataModel.SetLocalPlayerGameplayModifiers(
						modifierController.gameplayModifiers);
				}
			}
		}

		private void HandleGetMpPerPlayerPacket(GetMpPerPlayerPacket packet, IConnectedPlayer player)
		{
			_logger.Debug($"Received GetMpPerPlayerPacket from {player.userName}|{player.userId}");
			// Send MpPerPlayerPacket
			var ppPacket = new MpPerPlayerPacket();
			ppPacket.PPDEnabled = ppdt!.Value;
			ppPacket.PPMEnabled = ppmt!.Value;
			_multiplayerSessionManager.SendToPlayer(ppPacket, _multiplayerSessionManager.connectionOwner);
		}

		private void UpdateButtonsEnabled()
		{
			bool isPartyOwner = _gameServerLobbyFlowCoordinator._lobbyPlayerPermissionsModel.isPartyOwner;
			ppdt!.interactable = isPartyOwner;
			ppdt!.text.alpha = isPartyOwner ? 1f : 0.25f;
			ppmt!.interactable = isPartyOwner;
			ppmt!.text.alpha = isPartyOwner ? 1f : 0.25f;

			// Move toggles to correct position
			var locposition = _lobbyViewController._startGameReadyButton.gameObject.transform.localPosition;
			ppth!.gameObject.transform.localPosition = locposition;

			// Request updated button states from server
			_multiplayerSessionManager.SendToPlayer(new GetMpPerPlayerPacket(), _multiplayerSessionManager.connectionOwner);
		}

		private void SetLobbyState(MultiplayerLobbyState lobbyState)
		{
			_logger.Debug($"Current Lobby State {lobbyState}");

			_difficultyCanvasGroup!.alpha = (lobbyState == MultiplayerLobbyState.LobbySetup ||
											lobbyState == MultiplayerLobbyState.LobbyCountdown) ? 1f : 0.25f;

			foreach (var cell in difficultyControl!.cells)
			{
				cell.interactable = lobbyState == MultiplayerLobbyState.LobbySetup ||
				                    lobbyState == MultiplayerLobbyState.LobbyCountdown;
			}

			if (_lobbyViewController._isPartyOwner)
			{
				ppdt!.interactable = lobbyState == MultiplayerLobbyState.LobbySetup ||
				                              lobbyState == MultiplayerLobbyState.LobbyCountdown;
				ppmt!.interactable = lobbyState == MultiplayerLobbyState.LobbySetup || 
				                              lobbyState == MultiplayerLobbyState.LobbyCountdown;

				ppdt!.text.alpha = (lobbyState == MultiplayerLobbyState.LobbySetup ||
				                    lobbyState == MultiplayerLobbyState.LobbyCountdown) ? 1f : 0.25f;

				ppmt!.text.alpha = (lobbyState == MultiplayerLobbyState.LobbySetup ||
				                   lobbyState == MultiplayerLobbyState.LobbyCountdown) ? 1f : 0.25f;
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
