﻿using System;
using System.Linq;
using System.Reflection;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Components.Settings;
using HMUI;
using MultiplayerCore.Beatmaps.Abstractions;
using MultiplayerCore.Helpers;
using UnityEngine;

namespace MultiplayerCore.UI
{
    internal class MpColorsUI : NotifiableBase
    {
        public const string ResourcePath = "MultiplayerCore.UI.ColorsUI.bsml";

        public event Action dismissedEvent = null!;

        private ColorSchemeView colorSchemeView = null!;
        private readonly Color voidColor = new Color(0.5f, 0.5f, 0.5f, 0.25f);

        private readonly LobbySetupViewController _lobbySetupViewController;

        [UIComponent("noteColorsToggle")]
        private ToggleSetting noteColorToggle;

        [UIComponent("environmentColorsToggle")]
        private ToggleSetting environmentColorToggle;

        [UIComponent("obstacleColorsToggle")]
        private ToggleSetting obstacleColorsToggle;

        internal MpColorsUI(
            LobbySetupViewController lobbySetupViewController)
        {
            _lobbySetupViewController = lobbySetupViewController;
        }

        [UIComponent("modal")]
        private readonly ModalView _modal = null!;

        private Vector3 _modalPosition;

        [UIComponent("selected-color")]
        private readonly RectTransform selectedColorTransform = null!;

        [UIValue("noteColors")]
        public bool NoteColors
        {
            get => SongCoreConfig.CustomSongNoteColors;
            set => SongCoreConfig.CustomSongNoteColors = value;
        }

        [UIValue("obstacleColors")]
        public bool ObstacleColors
        {
            get => SongCoreConfig.CustomSongObstacleColors;
            set => SongCoreConfig.CustomSongObstacleColors = value;
        }

        [UIValue("environmentColors")]
        public bool EnvironmentColors
        {
            get => SongCoreConfig.CustomSongEnvironmentColors;
            set => SongCoreConfig.CustomSongEnvironmentColors = value;
        }

        internal void ShowColors(DifficultyColors colors)
        {
            Parse();
            _modal.Show(true);
            SetColors(colors);

            // We do this to apply any changes to the toggles that might have been made from within SongCores UI
            noteColorToggle.Value = SongCoreConfig.CustomSongNoteColors;
            obstacleColorsToggle.Value = SongCoreConfig.CustomSongObstacleColors;
            environmentColorToggle.Value = SongCoreConfig.CustomSongEnvironmentColors;
        }

        private void Parse()
        {
            if (!_modal)
                BSMLParser.instance.Parse(BeatSaberMarkupLanguage.Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), ResourcePath), _lobbySetupViewController.GetComponentInChildren<LevelBar>().gameObject, this);
            _modal.transform.localPosition = _modalPosition;
        }

        [UIAction("#post-parse")]
        private void PostParse()
        {
            ColorSchemeView colorSchemeViewPrefab = GameObject.Instantiate(Resources.FindObjectsOfTypeAll<ColorSchemeView>().First(), selectedColorTransform);
            colorSchemeView = IPA.Utilities.ReflectionUtil.CopyComponent<ColorSchemeView>(colorSchemeViewPrefab, colorSchemeViewPrefab.gameObject);
            GameObject.DestroyImmediate(colorSchemeViewPrefab);
            _modalPosition = _modal.transform.localPosition;
            _modal.blockerClickedEvent += Dismiss;
        }

        private void Dismiss()
            => _modal.Hide(false, dismissedEvent);

        private void SetColors(DifficultyColors colors)
        {
            Color saberLeft = colors._colorLeft == null ? voidColor : SongCore.Utilities.Utils.ColorFromMapColor(colors._colorLeft);
            Color saberRight = colors._colorRight == null ? voidColor : SongCore.Utilities.Utils.ColorFromMapColor(colors._colorRight);
            Color envLeft = colors._envColorLeft == null ? voidColor : SongCore.Utilities.Utils.ColorFromMapColor(colors._envColorLeft);
            Color envRight = colors._envColorRight == null ? voidColor : SongCore.Utilities.Utils.ColorFromMapColor(colors._envColorRight);
            Color envLeftBoost = colors._envColorLeftBoost == null ? voidColor : SongCore.Utilities.Utils.ColorFromMapColor(colors._envColorLeftBoost);
            Color envRightBoost = colors._envColorRightBoost == null ? voidColor : SongCore.Utilities.Utils.ColorFromMapColor(colors._envColorRightBoost);
            Color obstacle = colors._obstacleColor == null ? voidColor : SongCore.Utilities.Utils.ColorFromMapColor(colors._obstacleColor);

            colorSchemeView.SetColors(saberLeft, saberRight, envLeft, envRight, envLeftBoost, envRightBoost, obstacle);
        }
    }
}
