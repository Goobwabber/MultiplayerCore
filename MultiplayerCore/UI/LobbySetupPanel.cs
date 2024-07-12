using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
using MultiplayerCore.Beatmaps.Providers;
using UnityEngine;
using Zenject;

namespace MultiplayerCore.UI
{
	public class LobbySetupPanel : BSMLResourceViewController
	{
		public override string ResourceName => "MultiplayerCore.UI.LobbySetupPanel.bsml";
		private LobbySetupViewController _lobbyViewController;
		private MpBeatmapLevelProvider _beatmapLevelProvider;
		private ILobbyGameStateController _gameStateController;
		private bool _perPlayerDiffs = false;
		private bool _perPlayerModifiers = false;

		//BeatSaberMarkupLanguage.Tags.TextSegmentedControlTag

		[Inject]
		internal void Inject(LobbySetupViewController lobbyViewController, MpBeatmapLevelProvider beatmapLevelProvider, ILobbyGameStateController gameStateController)
		{
			DidActivate(true, false, false);
			_lobbyViewController = lobbyViewController;
			_beatmapLevelProvider = beatmapLevelProvider;
			_gameStateController = gameStateController;

			_lobbyViewController.didActivateEvent += DidActivate;
			_lobbyViewController.didDeactivateEvent += DidDeactivate;
		}

		protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
		{
			Plugin.Logger.Debug($"LobbySetup DidActivate called!");
			base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);

			if (_gameStateController == null || _lobbyViewController == null || perPlayerDiffsToggle == null ||
			    perPlayerModifiersToggle == null)
			{
				Plugin.Logger.Debug($"One object was null {_gameStateController}, {_lobbyViewController}, {perPlayerDiffsToggle}, {perPlayerModifiersToggle}");
				return;
			}

			_gameStateController.lobbyStateChangedEvent += SetLobbyState;

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
			Plugin.Logger.Debug($"LobbySetup DidDeactive called!");
			_gameStateController.lobbyStateChangedEvent -= SetLobbyState;
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


		#region UIComponents

		[UIComponent("ppdt")]
		public ToggleSetting perPlayerDiffsToggle;

		[UIComponent("ppmt")]
		public ToggleSetting perPlayerModifiersToggle;

		[UIComponent("difficulty-control")]
		public TextSegmentedControl difficulty;

		#endregion

		#region UIValues

		[UIValue("per-player-diffs")]
		public bool PerPlayerDifficulty
		{
			get => _perPlayerDiffs;
			set
			{
				_perPlayerDiffs = value;
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
				NotifyPropertyChanged();
			}
		}

		[UIValue("diffs")]
		private List<object> diffList { get; set; } = new() { "Easy", "Normal", "Hard", "Expert", "Expert+" };

		[UIAction("difficulty-selected")]
		public void SetSelectedDifficulty(TextSegmentedControl _, int index)
		{
			Plugin.Logger.Debug($"Selected difficulty {index}");
		}
		#endregion
	}
}
