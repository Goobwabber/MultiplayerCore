using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
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

			var csgHH = _lobbyViewController._cantStartGameHoverHint.transform.position;
			csgHH.y -= 0.4f;
			_lobbyViewController._cantStartGameHoverHint.transform.position = csgHH;

			_packetSerializer.RegisterType<MpPerPlayerPacket>();

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
			//var lobbyPlayersDataModel = _gameServerLobbyFlowCoordinator._lobbyPlayersDataModel as LobbyPlayersDataModel;
			//LobbyPlayerData? playerData =
			//	lobbyPlayersDataModel?.GetOrCreateLobbyPlayerDataModel(lobbyPlayersDataModel.localUserId,
			//		out _);
			//if (playerData != null && playerData.beatmapKey.IsValid())
			//	_currentBeatmapKey = playerData.beatmapKey;
			//else if (_gameStateController.selectedLevelGameplaySetupData.beatmapKey.IsValid())
			//	_currentBeatmapKey = _gameStateController.selectedLevelGameplaySetupData.beatmapKey;
			//if (_currentBeatmapKey.IsValid()) UpdateDifficultyList(_currentBeatmapKey);
			//else segmentVert.gameObject.SetActive(false);

			// We register the callbacks after setting the initial values
			_gameStateController.lobbyStateChangedEvent += SetLobbyState;
			//_gameStateController.selectedLevelGameplaySetupDataChangedEvent += HostSelectedBeatmap;
			_gameServerLobbyFlowCoordinator._multiplayerLevelSelectionFlowCoordinator.didSelectLevelEvent += LocalSelectedBeatmap;

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
			}
		}

		private void UpdateDifficultyList(BeatmapKey beatmapKey)
		{
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
				UpdateDifficultyList(_beatmapLevelsModel.GetBeatmapLevel(beatmapKey.levelId).GetDifficulties(beatmapKey.beatmapCharacteristic).ToList());
			}
		}

		private void UpdateDifficultyList(IReadOnlyList<BeatmapDifficulty> difficulties)
		{
			_allowedDiffs = (from diff in difficulties
							 where _gameServerLobbyFlowCoordinator._unifiedNetworkPlayerModel.selectionMask.difficulties
								 .Contains(diff)
							 select diff.ToString().Replace("ExpertPlus", "Expert+")
							 ).ToList();
			foreach (var difficulty in _allowedDiffs)
				Plugin.Logger.Debug($"Allowed difficulty='{difficulty}'");

			if (_allowedDiffs.Count > 1)
			{
				Plugin.Logger.Debug($"Setting texts");
				difficulty.SetTexts(_allowedDiffs);
				Plugin.Logger.Debug("Enabling gameObject");
				segmentVert.gameObject.SetActive(true);
			}
			else segmentVert.gameObject.SetActive(false);
		}

		private void SetLobbyState(MultiplayerLobbyState lobbyState)
		{
			if (_lobbyViewController == null)
				return;

			if (_lobbyViewController._isPartyOwner)
			{
				perPlayerDiffsToggle.interactable = true;
				perPlayerDiffsToggle.gameObject.SetActive(lobbyState == MultiplayerLobbyState.LobbySetup);

				perPlayerModifiersToggle.interactable = true;
				perPlayerModifiersToggle.gameObject.SetActive(lobbyState == MultiplayerLobbyState.LobbySetup);
			}
			difficulty.enabled = lobbyState == MultiplayerLobbyState.LobbySetup ||
			                     lobbyState == MultiplayerLobbyState.LobbyCountdown;
		}

		private void LocalSelectedBeatmap(LevelSelectionFlowCoordinator.State state)
		{
			_currentBeatmapKey = state.beatmapKey;
			UpdateDifficultyList(state.beatmapLevel.GetDifficulties(state.beatmapKey.beatmapCharacteristic).ToList());
			difficulty.SelectCellWithNumber(_allowedDiffs.IndexOf(_currentBeatmapKey.difficulty.ToString().Replace("ExpertPlus", "Expert+")));
			//var lobbyPlayersDataModel = _gameServerLobbyFlowCoordinator._lobbyPlayersDataModel as LobbyPlayersDataModel;
			//LobbyPlayerData? playerData =
			//	lobbyPlayersDataModel?.GetOrCreateLobbyPlayerDataModel(lobbyPlayersDataModel.localUserId,
			//		out _);
			//if (playerData != null && playerData.beatmapKey.IsValid())
			//	UpdateDifficultyList(playerData.beatmapKey);
		}

		private void HostSelectedBeatmap(ILevelGameplaySetupData gameplaySetupData)
		{
			UpdateDifficultyList(gameplaySetupData.beatmapKey);
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
				Plugin.Logger.Debug($"Sending MpPerPlayerPacket Packet with values: PPDEnabled='{_perPlayerDiffs}', PPMEnabled='{_perPlayerModifiers}'");
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
				Plugin.Logger.Debug($"Sending MpPerPlayerPacket Packet with values: PPDEnabled='{_perPlayerDiffs}', PPMEnabled='{_perPlayerModifiers}'");
				NotifyPropertyChanged();
			}
		}

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
