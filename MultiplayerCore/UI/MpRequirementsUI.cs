using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using HMUI;
using IPA.Utilities;
using MultiplayerCore.Beatmaps.Packets;
using MultiplayerCore.Objects;
using SiraUtil.Logging;
using UnityEngine;
using UnityEngine.UIElements;
using Zenject;
using static IPA.Logging.Logger;

namespace MultiplayerCore.UI
{
    internal class MpRequirementsUI : NotifiableBase, IInitializable, IDisposable
    {
        public const string ButtonResourcePath = "MultiplayerCore.UI.RequirementsButton.bsml";
        public const string ResourcePath = "MultiplayerCore.UI.RequirementsUI.bsml";

        internal Sprite? HaveReqIcon => SongCore.UI.RequirementsUI.instance.GetField<Sprite?, SongCore.UI.RequirementsUI>("HaveReqIcon");
        internal Sprite? MissingReqIcon => SongCore.UI.RequirementsUI.instance.GetField<Sprite?, SongCore.UI.RequirementsUI>("MissingReqIcon");
        internal Sprite? HaveSuggestionIcon => SongCore.UI.RequirementsUI.instance.GetField<Sprite?, SongCore.UI.RequirementsUI>("HaveSuggestionIcon");
        internal Sprite? MissingSuggestionIcon => SongCore.UI.RequirementsUI.instance.GetField<Sprite?, SongCore.UI.RequirementsUI>("MissingSuggestionIcon");
        internal Sprite? WarningIcon => SongCore.UI.RequirementsUI.instance.GetField<Sprite?, SongCore.UI.RequirementsUI>("WarningIcon");
        internal Sprite? InfoIcon => SongCore.UI.RequirementsUI.instance.GetField<Sprite?, SongCore.UI.RequirementsUI>("InfoIcon");
        internal Sprite? ColorsIcon => SongCore.UI.RequirementsUI.instance.GetField<Sprite?, SongCore.UI.RequirementsUI>("ColorsIcon");

        private bool _buttonGlow = false;
        private bool _buttonInteractable = false;
        private ImageView _buttonBG = null!;
        private Color _originalColor0;
        private Color _originalColor1;

        private readonly LobbySetupViewController _lobbySetupViewController;
        private readonly ILobbyPlayersDataModel _playersDataModel;
        private readonly MpColorsUI _colorsUI;
        private readonly BeatmapLevelsModel _beatmapLevelsModel;
        private readonly SiraLog _logger;

        private readonly List<CustomListTableData.CustomCellInfo> _unusedCells = new();
        private readonly List<CustomListTableData.CustomCellInfo> _levelInfoCells = new();

        internal MpRequirementsUI(
            LobbySetupViewController lobbySetupViewController,
            ILobbyPlayersDataModel playersDataModel,
            MpColorsUI colorsUI,
            BeatmapLevelsModel beatmapLevelsModel,
            SiraLog logger)
        {
            _lobbySetupViewController = lobbySetupViewController;
            _playersDataModel = playersDataModel;
            _colorsUI = colorsUI;
            _beatmapLevelsModel = beatmapLevelsModel;
            _logger = logger;
        }

        [UIComponent("list")]
        public CustomListTableData customListTableData = null!;

        [UIValue("button-glow")]
        public bool ButtonGlowColor
        {
            get => _buttonGlow;
            set
            {
                _buttonGlow = value;
                NotifyPropertyChanged();
            }
        }

        [UIValue("button-interactable")]
        public bool ButtonInteractable
        {
            get => _buttonInteractable;
            set
            {
                _buttonInteractable = value;
                NotifyPropertyChanged();
            }
        }

        [UIComponent("modal")]
        private ModalView _modal = null!;

        private Vector3 _modalPosition;

        [UIComponent("info-button")]
        private Transform _infoButtonTransform = null!;

        [UIComponent("root")]
        protected readonly RectTransform _root = null!;

        public void Initialize()
        {
            var bar = _lobbySetupViewController.GetComponentInChildren<EditableBeatmapSelectionView>().transform.Find("LevelBarSimple").GetComponent<LevelBar>();
            BSMLParser.instance.Parse(BeatSaberMarkupLanguage.Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), ButtonResourcePath), bar.gameObject, this);
            _infoButtonTransform.localScale *= 0.7f;
            _infoButtonTransform.gameObject.SetActive(true);
            _buttonBG = _infoButtonTransform.Find("BG").GetComponent<ImageView>();
            _originalColor0 = _buttonBG.color0;
            _originalColor1 = _buttonBG.color1;

            _playersDataModel.didChangeEvent += BeatmapSelected;
            _colorsUI.dismissedEvent += ColorsDismissed;

            BSMLParser.instance.Parse(BeatSaberMarkupLanguage.Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), ResourcePath), _root.gameObject, this);
            _modalPosition = _modal!.transform.localPosition;
        }

        public void Dispose()
        {
            _playersDataModel.didChangeEvent -= BeatmapSelected;
            _colorsUI.dismissedEvent -= ColorsDismissed;
        }

        private void BeatmapSelected(string _)
        {
            var key = _playersDataModel[_playersDataModel.localUserId].beatmapKey;
            if (!key.IsValid()) return;

            var levelId = key.levelId;
            var localLevel = _beatmapLevelsModel.GetBeatmapLevel(levelId);
            if (localLevel != null) // we have a local level to set info from
            {
                SetRequirementsFromLevel(localLevel, key);
                return;
            }

            if (_playersDataModel is MpPlayersDataModel mpPlayersDataModel)
            {
                var levelHash = Utilities.HashForLevelID(levelId);
                var packet = mpPlayersDataModel.FindLevelPacket(levelHash);
                if (packet != null) // we have a packet to set info from
                {
                    SetRequirementsFromPacket(packet);
                    return;
                }
            }

            SetNoRequirementsFound(); // nothing found
        }

        private void SetRequirementsFromLevel(BeatmapLevel level, in BeatmapKey key)
        {
            ClearCells(_levelInfoCells);

            var levelHash = Utilities.HashForLevelID(key.levelId);
            if (!string.IsNullOrEmpty(levelHash))
            {
                var extraSongData = SongCore.Collections.RetrieveExtraSongData(levelHash!);
                if (extraSongData != null)
                {
                    var diffData = SongCore.Collections.RetrieveDifficultyData(level, key);
                    if (diffData != null && diffData.additionalDifficultyData != null && diffData.additionalDifficultyData._requirements != null)
                    {
                        foreach (var req in diffData.additionalDifficultyData._requirements)
                        {
                            var cell = GetCellInfo();
                            bool installed = SongCore.Collections.capabilities.Contains(req);
                            cell.text = $"<size=75%>{req}";
                            cell.subtext = installed ? "Requirement found" : "Requirement missing";
                            cell.icon = installed ? HaveReqIcon : MissingReqIcon;
                            _levelInfoCells.Add(cell);
                        }
                    }

                    foreach (var contributor in extraSongData.contributors) 
                    {
                        var cell = GetCellInfo();
                        cell.text = $"<size=75%>{contributor._name}";
                        cell.subtext = contributor._role;
                        cell.icon = InfoIcon;
                        _levelInfoCells.Add(cell);
                    }

                    if (diffData != null && 
                            !( // check all colors for null, if all are null just ignore
                                diffData._colorLeft != null ||
                                diffData._colorRight != null ||
                                diffData._envColorLeft != null ||
                                diffData._envColorLeftBoost != null ||
                                diffData._envColorRight != null ||
                                diffData._envColorRightBoost != null ||
                                diffData._obstacleColor != null
                            )
                        )
                    {
                        var cell = GetCellInfo();
                        cell.text = "<size=75%>Custom Colors Available";
                        cell.subtext = "Click here to preview";
                        cell.icon = ColorsIcon;
                        _levelInfoCells.Add(cell);

                        _colorsUI.AcceptColors(
                            diffData._colorLeft,
                            diffData._colorRight,
                            diffData._envColorLeft,
                            diffData._envColorLeftBoost,
                            diffData._envColorRight,
                            diffData._envColorRightBoost,
                            diffData._obstacleColor
                        );
                    }
                }
            }

            UpdateData();
        }

        private void SetRequirementsFromPacket(MpBeatmapPacket packet)
        {
            ClearCells(_levelInfoCells);

            var diff = packet.difficulty;
            if (packet.requirements.ContainsKey(diff))
            {
				foreach (var req in packet.requirements[diff])
				{
					var cell = GetCellInfo();
					bool installed = SongCore.Collections.capabilities.Contains(req);
					cell.text = $"<size=75%>{req}";
					cell.subtext = installed ? "Requirement found" : "Requirement missing";
					cell.icon = installed ? HaveReqIcon : MissingReqIcon;
					_levelInfoCells.Add(cell);
				}
			}

			foreach (var contributor in packet.contributors)
            {
                var cell = GetCellInfo();
                cell.text = $"<size=75%>{contributor._name}";
                cell.subtext = contributor._role;
                cell.icon = InfoIcon;
                _levelInfoCells.Add(cell);
            }

            if (packet.mapColors.TryGetValue(diff, out var mapColors) && mapColors.AnyAreNotNull) 
            {
                var cell = GetCellInfo();
                cell.text = "<size=75%>Custom Colors Available";
                cell.subtext = "Click here to preview";
                cell.icon = ColorsIcon;
                _levelInfoCells.Add(cell);

                _colorsUI.AcceptColors(mapColors);
            }

            UpdateData();
        }

        private void SetNoRequirementsFound()
        {
            ClearCells(_levelInfoCells);

            UpdateData();
        }

        private void ClearCells(List<CustomListTableData.CustomCellInfo> cells)
        {
            foreach (var cell in cells) _unusedCells.Add(cell);
            cells.Clear();
        }

        private CustomListTableData.CustomCellInfo GetCellInfo()
        {
            if (_unusedCells.Count == 0) return new CustomListTableData.CustomCellInfo(String.Empty, String.Empty, null);
            var cell = _unusedCells[0];
            _unusedCells.RemoveAt(0);
            return cell;
        }

        private void UpdateData()
        {
            customListTableData.data.Clear();

            foreach (var cell in _levelInfoCells) customListTableData.data.Add(cell);

            customListTableData.tableView.ReloadData();
            customListTableData.tableView.ScrollToCellWithIdx(0, TableView.ScrollPositionType.Beginning, false);

            UpdateRequirementButton();
        }

        // if there is data, we should have the button be active
        private void UpdateRequirementButton() => ButtonInteractable = customListTableData.data.Count > 0;

        private void ColorsDismissed() => ShowRequirements();

        [UIAction("button-click")]
        internal void ShowRequirements()
        {
            _modal.transform.localPosition = _modalPosition;
            _modal.Show(true);
        }

        [UIAction("list-select")]
        private void Select(TableView _, int index)
        {
            var localUserData = _playersDataModel[_playersDataModel.localUserId];
            var beatmapLevel = localUserData.beatmapKey;

            var cell = customListTableData.data[index];
            if (cell.icon == ColorsIcon) _modal.Hide(false, () => _colorsUI.ShowColors());

            customListTableData.tableView.ClearSelection();
        }
    }
}
