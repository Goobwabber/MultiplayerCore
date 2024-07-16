using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
using IPA.Utilities.Async;
using MultiplayerCore.Beatmaps.Abstractions;
using MultiplayerCore.Beatmaps.Providers;
using MultiplayerCore.Networking;
using MultiplayerCore.Players.Packets;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;
using Zenject;
using static IPA.Logging.Logger;

namespace MultiplayerCore.UI
{
	public class LobbySetupPanel : BSMLResourceViewController
	{
		public override string ResourceName => "MultiplayerCore.UI.LobbySetupPanel.bsml";
		private GameServerLobbyFlowCoordinator _gameServerLobbyFlowCoordinator;
		private LobbySetupViewController _lobbyViewController;
		private BeatmapLevelsModel _beatmapLevelsModel;
		private IMultiplayerSessionManager _multiplayerSessionManager;
		private ILobbyGameStateController _gameStateController;
		private MpPacketSerializer _packetSerializer;
		private MpBeatmapLevelProvider _beatmapLevelProvider;
		private BeatmapKey _currentBeatmapKey;
		private bool _perPlayerDiffs = false;
		private bool _perPlayerModifiers = false;
		private List<string> _allowedDiffs;
		private CanvasGroup? _difficultyCanvasGroup;

		//BeatSaberMarkupLanguage.Tags.TextSegmentedControlTag

		[Inject]
		internal void Inject(GameServerLobbyFlowCoordinator gameServerLobbyFlowCoordinator, BeatmapLevelsModel beatmapLevelsModel, IMultiplayerSessionManager sessionManager, MpBeatmapLevelProvider beatmapLevelProvider, MpPacketSerializer packetSerializer)
		{
			DidActivate(true, false, false);
			_gameServerLobbyFlowCoordinator = gameServerLobbyFlowCoordinator;
			_lobbyViewController = _gameServerLobbyFlowCoordinator._lobbySetupViewController;
			_gameStateController = _gameServerLobbyFlowCoordinator._lobbyGameStateController;
			_beatmapLevelsModel = beatmapLevelsModel;
			_multiplayerSessionManager = sessionManager;
			_beatmapLevelProvider = beatmapLevelProvider;
			_packetSerializer = packetSerializer;

			_lobbyViewController.didActivateEvent += DidActivate;
			_lobbyViewController.didDeactivateEvent += DidDeactivate;


			// TODO: Possibly adjust position based on enabled UI elements
			var cgubPos = _lobbyViewController._cancelGameUnreadyButton.transform.position;
			cgubPos.y -= 0.4f;
			_lobbyViewController._cancelGameUnreadyButton.transform.position = cgubPos;

			var sgrbPos = _lobbyViewController._startGameReadyButton.transform.position;
			sgrbPos.y -= 0.4f;
			_lobbyViewController._startGameReadyButton.transform.position = sgrbPos;

			var csgHHPos = _lobbyViewController._cantStartGameHoverHint.transform.position;
			csgHHPos.y -= 0.4f;
			_lobbyViewController._cantStartGameHoverHint.transform.position = csgHHPos;

			//if (!_lobbyViewController._isPartyOwner)
			//{
			//	var diffPos = difficulty.transform.position;
			//	diffPos.y -= -0.2f;

			//}

			// TODO: Proper registration
			_packetSerializer.RegisterCallback<MpPerPlayerPacket>(HandleMpPerPlayerPacket);
			_packetSerializer.RegisterType<GetMpPerPlayerPacket>();

			//var stwParentPos = _lobbyViewController._spectatorWarningTextWrapper.transform.position;
			//stwParentPos.y -= 0.5f;
			//_lobbyViewController._spectatorWarningTextWrapper.transform.position = stwParentPos;
		}

		protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
		{
			Plugin.Logger.Debug($"LobbySetup DidActivate called! {firstActivation}, {addedToHierarchy}, {screenSystemEnabling}");
			base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);

			if (_gameStateController == null || _lobbyViewController == null || perPlayerDiffsToggle == null ||
			    perPlayerModifiersToggle == null)
			{
				Plugin.Logger.Debug($"One object was null {_gameStateController}, {_lobbyViewController}, {perPlayerDiffsToggle}, {perPlayerModifiersToggle}");
				return;
			}
			// Make sure to only allow selecting difficulties that are enabled for the lobby
			if (!firstActivation)
			{
				var lobbyPlayersDataModel = _gameServerLobbyFlowCoordinator._lobbyPlayersDataModel as LobbyPlayersDataModel;
				LobbyPlayerData? playerData =
					lobbyPlayersDataModel?.GetOrCreateLobbyPlayerDataModel(lobbyPlayersDataModel.localUserId,
						out _);
				if (playerData != null)
					UpdateDifficultyList(playerData.beatmapKey);
			}
			
			if (!firstActivation && addedToHierarchy)
			{
				// Reset our buttons
				_perPlayerDiffs = false;
				_perPlayerModifiers = false;
				UpdateButtonValues();
				// Request Updated state
				_multiplayerSessionManager.Send(new GetMpPerPlayerPacket());
			}
			//else if (_gameStateController.selectedLevelGameplaySetupData.beatmapKey.IsValid())
			//	_currentBeatmapKey = _gameStateController.selectedLevelGameplaySetupData.beatmapKey;
			//if (_currentBeatmapKey.IsValid()) UpdateDifficultyList(_currentBeatmapKey);
			//else segmentVert.gameObject.SetActive(false);

			// We register the callbacks
			_gameStateController.lobbyStateChangedEvent += SetLobbyState;
			//_gameStateController.selectedLevelGameplaySetupDataChangedEvent += HostSelectedBeatmap;
			_gameServerLobbyFlowCoordinator._multiplayerLevelSelectionFlowCoordinator.didSelectLevelEvent += LocalSelectedBeatmap;
			_gameServerLobbyFlowCoordinator._serverPlayerListViewController.selectSuggestedBeatmapEvent += UpdateDifficultyList;
			_lobbyViewController.clearSuggestedBeatmapEvent += ClearLocalSelectedBeatmap;
			_gameServerLobbyFlowCoordinator._lobbyPlayerPermissionsModel.permissionsChangedEvent +=
				UpdateButtonsEnabled;

			//else UpdateDifficultyList(Enum.GetValues(typeof(BeatmapDifficulty)).Cast<BeatmapDifficulty>().ToList());

			//List<BeatmapDifficulty> difficultyList =
			//	Enum.GetValues(typeof(BeatmapDifficulty)).Cast<BeatmapDifficulty>().ToList();
			//var levelId = _gameStateController.selectedLevelGameplaySetupData.beatmapKey.levelId;
			//var characteristic = _gameStateController.selectedLevelGameplaySetupData.beatmapKey.beatmapCharacteristic;
			//_allowedDiffs = (from diff in difficultyList
			//	where _gameServerLobbyFlowCoordinator._unifiedNetworkPlayerModel.selectionMask.difficulties
			//		.Contains(diff)
			//	select diff.ToString().Replace("ExpertPlus", "Expert+")).ToList();
			//foreach (var difficulty in _allowedDiffs)
			//	Plugin.Logger.Debug($"Allowed difficulty='{difficulty}'");

			if (_lobbyViewController._isPartyOwner)
			{
				perPlayerDiffsToggle.gameObject.SetActive(true);
				perPlayerModifiersToggle.gameObject.SetActive(true);
				perPlayerDiffsToggle.interactable = true;
				perPlayerModifiersToggle.interactable = true;
			}
			else
			{
				perPlayerDiffsToggle.gameObject.SetActive(false);
				perPlayerModifiersToggle.gameObject.SetActive(false);
			}
		}

		protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
		{
			base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
			Plugin.Logger.Debug($"LobbySetup DidDeactivate called! {removedFromHierarchy}, {screenSystemDisabling}");
			if (removedFromHierarchy)
			{
				_gameStateController.lobbyStateChangedEvent -= SetLobbyState;
				//_gameStateController.selectedLevelGameplaySetupDataChangedEvent -= HostSelectedBeatmap;
				_gameServerLobbyFlowCoordinator._multiplayerLevelSelectionFlowCoordinator.didSelectLevelEvent -= LocalSelectedBeatmap;
				_gameServerLobbyFlowCoordinator._serverPlayerListViewController.selectSuggestedBeatmapEvent -= UpdateDifficultyList;
				_lobbyViewController.clearSuggestedBeatmapEvent -= ClearLocalSelectedBeatmap;
				_gameServerLobbyFlowCoordinator._lobbyPlayerPermissionsModel.permissionsChangedEvent -=
					UpdateButtonsEnabled;
			}
		}

		private void HandleMpPerPlayerPacket(MpPerPlayerPacket packet, IConnectedPlayer player)
		{
			Plugin.Logger.Debug($"Got MpPerPlayerPacket from {player.userName}|{player.userId} with values PPDEnabled={packet.PPDEnabled}, PPMEnabled={packet.PPMEnabled}");
			if (packet.PPDEnabled != PerPlayerDifficulty || packet.PPMEnabled != _perPlayerModifiers)
			{
				_perPlayerDiffs = packet.PPDEnabled;
				_perPlayerModifiers = packet.PPMEnabled;
				//perPlayerDiffsToggle.Value = _perPlayerDiffs;
				//perPlayerModifiersToggle.Value = _perPlayerModifiers;
				UpdateButtonValues();
			}
		}

		public string DiffToStr(BeatmapDifficulty difficulty)
		{
			return difficulty == BeatmapDifficulty.ExpertPlus ? "Expert+" : difficulty.ToString();
		}

		//public List<string> DiffsToStrs(BeatmapDifficulty[] difficulties) =>
		//	difficulties.Select(diff => DiffToStr(diff)).ToList();

		private void UpdateDifficultyList(BeatmapKey beatmapKey)
		{
			_currentBeatmapKey = beatmapKey;
			if (!_currentBeatmapKey.IsValid())
			{
				segmentVert.gameObject.SetActive(false);
				return;
			}
			var levelHash = Utilities.HashForLevelID(beatmapKey.levelId);
			if (levelHash != null)
			{
				Plugin.Logger.Debug($"Level is custom, trying to get beatmap for hash {levelHash}");
				_beatmapLevelProvider.GetBeatmap(levelHash).ContinueWith(levelTask =>
				{
					if (levelTask.IsCompleted && !levelTask.IsFaulted && levelTask.Result != null)
					{
						var level = levelTask.Result;
						Plugin.Logger.Debug($"Got level {level.LevelHash}, {level.Requirements}, {level.Requirements[beatmapKey.beatmapCharacteristic.serializedName]}");
						// Hacky we use requirements to get the available difficulties
						UpdateDifficultyList(level.Requirements[beatmapKey.beatmapCharacteristic.serializedName].Keys
							.ToList());
					}
				});
			}
			else
			{
				Plugin.Logger.Debug($"LevelId not custom: {beatmapKey.levelId}, getting difficulties from basegame");
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
						Plugin.Logger.Debug($"Allowed difficulty='{difficultyStr}'");

					if (_allowedDiffs.Count > 1)
					{
						segmentVert.gameObject.SetActive(true);
						difficulty.SetTexts(_allowedDiffs);
						int index = _allowedDiffs.IndexOf(DiffToStr(_currentBeatmapKey.difficulty));
						if (index > 0)
							difficulty.SelectCellWithNumber(index);
					}
					else segmentVert.gameObject.SetActive(false);
				}
			);
		}

		private void SetLobbyState(MultiplayerLobbyState lobbyState)
		{
			Plugin.Logger.Debug($"Current Lobby State {lobbyState}");
			enableUserInteractions = lobbyState == MultiplayerLobbyState.LobbySetup;

			if (_difficultyCanvasGroup == null)
				_difficultyCanvasGroup = difficulty?.gameObject.AddComponent<CanvasGroup>();
			if (_difficultyCanvasGroup != null)
				_difficultyCanvasGroup.alpha = lobbyState == MultiplayerLobbyState.LobbySetup ? 1f : 0.25f;
			//var canvasGroup = segmentVert.GetComponentInParent<CanvasGroup>();
			//var ourCanvasGroup = GetComponent<CanvasGroup>();
			//if (ourCanvasGroup != null)
			//	Plugin.Logger.Debug($"CanvasGroup found in parent");
			//else Plugin.Logger.Error($"CanvasGroup was null!");
			//if (ourCanvasGroup != null)
			//	//UnityMainThreadTaskScheduler.Factory.StartNew(async () =>
			//	//{
			//		//await Task.Delay(2000);
			//	ourCanvasGroup.alpha = lobbyState == MultiplayerLobbyState.LobbySetup ? 1f : 0.25f;
			//	//});
			//segmentVert. = (lobbyState == MultiplayerLobbyState.LobbySetup);
			if (_lobbyViewController == null)
				return;

			if (_lobbyViewController._isPartyOwner)
			{
				perPlayerDiffsToggle.interactable = lobbyState == MultiplayerLobbyState.LobbySetup;
				//perPlayerDiffsToggle.gameObject.SetActive(lobbyState == MultiplayerLobbyState.LobbySetup);

				perPlayerModifiersToggle.interactable = lobbyState == MultiplayerLobbyState.LobbySetup;
				//perPlayerModifiersToggle.gameObject.SetActive(lobbyState == MultiplayerLobbyState.LobbySetup);

			}
		}

		private void LocalSelectedBeatmap(LevelSelectionFlowCoordinator.State state)
		{
			_currentBeatmapKey = state.beatmapKey;
			UpdateDifficultyList(state.beatmapLevel.GetDifficulties(state.beatmapKey.beatmapCharacteristic).ToList());
			//var lobbyPlayersDataModel = _gameServerLobbyFlowCoordinator._lobbyPlayersDataModel as LobbyPlayersDataModel;
			//LobbyPlayerData? playerData =
			//	lobbyPlayersDataModel?.GetOrCreateLobbyPlayerDataModel(lobbyPlayersDataModel.localUserId,
			//		out _);
			//if (playerData != null && playerData.beatmapKey.IsValid())
			//	UpdateDifficultyList(playerData.beatmapKey);
		}

		//private void HostSelectedBeatmap(ILevelGameplaySetupData gameplaySetupData)
		//{
		//	UpdateDifficultyList(gameplaySetupData.beatmapKey);
		//}

		private void ClearLocalSelectedBeatmap()
		{
			segmentVert.gameObject.SetActive(false);
			_currentBeatmapKey = new BeatmapKey();
		}

		private void UpdateButtonsEnabled()
		{
			if (_lobbyViewController._isPartyOwner)
			{
				perPlayerDiffsToggle.gameObject.SetActive(true);
				perPlayerModifiersToggle.gameObject.SetActive(true);
			}
			else
			{
				perPlayerDiffsToggle.gameObject.SetActive(false);
				perPlayerModifiersToggle.gameObject.SetActive(false);
			}
		}

		private void UpdateButtonValues()
		{
			perPlayerDiffsToggle.Value = _perPlayerDiffs;
			perPlayerModifiersToggle.Value = _perPlayerModifiers;
		}


		#region UIComponents

		[UIComponent("ppdt")]
		public ToggleSetting perPlayerDiffsToggle;

		[UIComponent("ppmt")]
		public ToggleSetting perPlayerModifiersToggle;

		[UIComponent("difficulty-control")]
		public TextSegmentedControl difficulty;

		[UIComponent("segment-vert")] 
		public VerticalLayoutGroup segmentVert;

		#endregion

		#region UIValues

		[UIValue("per-player-diffs")]
		public bool PerPlayerDifficulty
		{
			get => _perPlayerDiffs;
			set
			{
				_perPlayerDiffs = value;
				_multiplayerSessionManager.Send(new MpPerPlayerPacket
				{
					PPDEnabled = _perPlayerDiffs,
					PPMEnabled = _perPlayerModifiers
				});
				NotifyPropertyChanged();
			}
		}

		[UIValue("per-player-modifiers")]
		public bool PerPlayerModifiers
		{
			get => _perPlayerModifiers;
			set
			{
				_perPlayerModifiers = value;
				_multiplayerSessionManager.Send(new MpPerPlayerPacket
				{
					PPDEnabled = _perPlayerDiffs,
					PPMEnabled = _perPlayerModifiers
				});
				NotifyPropertyChanged();
			}
		}

		//[UIAction("per-player-modifiers-changed")]
		//public void OnPerPlayerModifiersChanged(bool value)
		//{
		//	_perPlayerModifiers = value;
		//	_multiplayerSessionManager.Send(new MpPerPlayerPacket
		//	{
		//		PPDEnabled = _perPlayerDiffs,
		//		PPMEnabled = _perPlayerModifiers
		//	});
		//	Plugin.Logger.Debug($"Sending MpPerPlayerPacket Packet with values: PPDEnabled='{_perPlayerDiffs}', PPMEnabled='{_perPlayerModifiers}'");
		//}

		//[UIAction("per-player-diffs-changed")]
		//public void OnPerPlayerDifficultyChanged(bool value)
		//{
		//	_perPlayerDiffs = value;
		//	_multiplayerSessionManager.Send(new MpPerPlayerPacket
		//	{
		//		PPDEnabled = _perPlayerDiffs,
		//		PPMEnabled = _perPlayerModifiers
		//	});
		//	Plugin.Logger.Debug($"Sending MpPerPlayerPacket Packet with values: PPDEnabled='{_perPlayerDiffs}', PPMEnabled='{_perPlayerModifiers}'");
		//}


		[UIAction("difficulty-selected")]
		public void SetSelectedDifficulty(TextSegmentedControl _, int index)
		{
			var diff = _allowedDiffs[index];
			Plugin.Logger.Debug($"Selected difficulty at {index} - {diff}");
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
