using System;
using System.Reflection;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using HMUI;
using IPA.Utilities;
using SiraUtil.Logging;
using UnityEngine;
using Zenject;

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
        private readonly SiraLog _logger;

        internal MpRequirementsUI(
            LobbySetupViewController lobbySetupViewController,
            ILobbyPlayersDataModel playersDataModel,
            MpColorsUI colorsUI,
            SiraLog logger)
        {
            _lobbySetupViewController = lobbySetupViewController;
            _playersDataModel = playersDataModel;
            _colorsUI = colorsUI;
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
        }

        public void Dispose()
        {
            _playersDataModel.didChangeEvent -= BeatmapSelected;
            _colorsUI.dismissedEvent -= ColorsDismissed;
        }

        private void BeatmapSelected(string a)
        {
            var beatmapLevel = _playersDataModel[_playersDataModel.localUserId].beatmapKey;
            // TODO: Load MpBeatmap data from somewhere
            // if (beatmapLevel?.beatmapLevel is MpBeatmap mpLevel)
            // {
            //     string characteristicName = null!;
            //     if (mpLevel.DifficultyColors.ContainsKey(beatmapLevel.beatmapCharacteristic.name))
            //         characteristicName = beatmapLevel.beatmapCharacteristic.name;
            //     else if (mpLevel.DifficultyColors.ContainsKey(beatmapLevel.beatmapCharacteristic.serializedName))
            //         characteristicName = beatmapLevel.beatmapCharacteristic.serializedName;
            //     if (characteristicName != null && mpLevel.DifficultyColors[characteristicName].TryGetValue(beatmapLevel.beatmapDifficulty, out var colors))
            //         ButtonInteractable = colors.AnyAreNotNull;
            //     else
            //         ButtonInteractable = (characteristicName != null && mpLevel.Requirements[characteristicName].Any()) || (mpLevel.Contributors?.Any() ?? false);
            // }
            // else
                ButtonInteractable = false;
        }

        private void ColorsDismissed()
            => ShowRequirements();

        [UIAction("button-click")]
        internal void ShowRequirements()
        {
            if (_modal == null)
            {
                BSMLParser.instance.Parse(BeatSaberMarkupLanguage.Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), ResourcePath), _root.gameObject, this);
                _modalPosition = _modal!.transform.localPosition;
            }

            _modal.transform.localPosition = _modalPosition;
            _modal.Show(true);
            customListTableData.data.Clear();

            var localUserId = _playersDataModel.localUserId;
            if (!_playersDataModel.ContainsKey(localUserId))
                return;
            var localPlayerDataModel = _playersDataModel[localUserId];
            var level = localPlayerDataModel.beatmapKey;

            // TODO: Load MpBeatmap data from somewhere
            // if (level.beatmapLevel is MpBeatmap mpLevel)
            // {
            //     string characteristicName = null!;
            //     if (mpLevel.Requirements.ContainsKey(level.beatmapCharacteristic.name) || mpLevel.DifficultyColors.ContainsKey(level.beatmapCharacteristic.name))
            //         characteristicName = level.beatmapCharacteristic.name;
            //     else if (mpLevel.Requirements.ContainsKey(level.beatmapCharacteristic.serializedName) || mpLevel.DifficultyColors.ContainsKey(level.beatmapCharacteristic.serializedName))
            //         characteristicName = level.beatmapCharacteristic.serializedName;
            //
            //     // Requirements
            //     if (mpLevel.Requirements.TryGetValue(characteristicName, out var difficultiesRequirements))
            //         if (difficultiesRequirements.TryGetValue(level.beatmapDifficulty, out var difficultyRequirements) && difficultyRequirements.Any())
            //             foreach (string req in difficultyRequirements)
            //                 customListTableData.data.Add(!SongCore.Collections.capabilities.Contains(req)
            //                     ? new CustomCellInfo($"<size=75%>{req}", "Missing Requirement", MissingReqIcon)
            //                     : new CustomCellInfo($"<size=75%>{req}", "Requirement", HaveReqIcon));
            //
            //     // Contributors
            //     if (mpLevel.Contributors != null)
            //         foreach (var contributor in mpLevel.Contributors)
            //         {
            //             if (contributor.icon == null)
            //             {
            //                 if (!string.IsNullOrWhiteSpace(contributor._iconPath) && !string.IsNullOrEmpty(contributor._iconPath) && SongCore.Collections.songWithHashPresent(mpLevel.LevelHash))
            //                 {
            //                     var songCoreLevel = SongCore.Loader.GetLevelByHash(mpLevel.LevelHash);
            //                     contributor.icon = SongCore.Utilities.Utils.LoadSpriteFromFile(Path.Combine(songCoreLevel!.customLevelPath, contributor._iconPath));
            //                     customListTableData.data.Add(new CustomCellInfo(contributor._name, contributor._role, contributor.icon != null ? contributor.icon : InfoIcon));
            //                 }
            //                 else
            //                     customListTableData.data.Add(new CustomCellInfo(contributor._name, contributor._role, InfoIcon));
            //             }
            //             else
            //                 customListTableData.data.Add(new CustomCellInfo(contributor._name, contributor._role, contributor.icon));
            //         }
            //
            //     // Colors
            //     var customColorsEnabled = SongCoreConfig.AnyCustomSongColors;
            //     if (mpLevel.DifficultyColors.TryGetValue(characteristicName, out var difficultyColors) && difficultyColors.TryGetValue(level.beatmapDifficulty, out var colors) && (colors.AnyAreNotNull))
            //         customListTableData.data.Add(new CustomCellInfo($"<size=75%>Custom Colors Available", $"Click here to preview & {(customColorsEnabled ? "disable" : "enable")} it.", ColorsIcon));
            //     else if (mpLevel is BeatSaverBeatmapLevel)
            //         customListTableData.data.Add(new CustomCellInfo($"<size=75%>Custom Colors", $"Click here to preview & {(customColorsEnabled ? "disable" : "enable")} it.", ColorsIcon));
            //
            //     customListTableData.tableView.ReloadData();
            //     customListTableData.tableView.ScrollToCellWithIdx(0, TableView.ScrollPositionType.Beginning, false);
            // }
        }

        [UIAction("list-select")]
        private void Select(TableView _, int index)
        {
            var localUserData = _playersDataModel[_playersDataModel.localUserId];
            var beatmapLevel = localUserData.beatmapKey;

            // TODO: Load MpBeatmap data from somewhere
            // if (beatmapLevel.beatmapLevel is MpBeatmap mpLevel)
            // {
            //     string characteristicName = null!;
            //     if (mpLevel.Requirements.ContainsKey(beatmapLevel.beatmapCharacteristic.name) || mpLevel.DifficultyColors.ContainsKey(beatmapLevel.beatmapCharacteristic.name))
            //         characteristicName = beatmapLevel.beatmapCharacteristic.name;
            //     else if (mpLevel.Requirements.ContainsKey(beatmapLevel.beatmapCharacteristic.serializedName) || mpLevel.DifficultyColors.ContainsKey(beatmapLevel.beatmapCharacteristic.serializedName))
            //         characteristicName = beatmapLevel.beatmapCharacteristic.serializedName;
            //
            //     customListTableData.tableView.ClearSelection();
            //     if (customListTableData.data[index].icon == ColorsIcon)
            //         _modal.Hide(false, () => _colorsUI.ShowColors(mpLevel.DifficultyColors[characteristicName][beatmapLevel.beatmapDifficulty]));
            // }
        }
    }
}
