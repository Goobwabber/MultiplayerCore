using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using HMUI;
using MultiplayerCore.Beatmaps.Abstractions;
using SongCore.UI;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Zenject;

namespace MultiplayerCore.UI
{
    internal class MpColorsUI : NotifiableBase
    {
        public const string ResourcePath = "MultiplayerCore.UI.ColorsUI.bsml";

        public event Action dismissedEvent = null!;

        private BoostedColorSchemeView boostedColorSchemeView = null!;
        private readonly Color voidColor = new Color(0.5f, 0.5f, 0.5f, 0.25f);

        private readonly LobbySetupViewController _lobbySetupViewController;

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

        [UIValue("colors")]
        public bool Colors
        {
            get
            {
                object sConfiguration = typeof(SongCore.Plugin).GetProperty("Configuration", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
                return (bool)typeof(SongCore.Plugin).Assembly.GetType("SongCore.SConfiguration").GetProperty("CustomSongColors").GetValue(sConfiguration);
            }
            set
            {
                object sConfiguration = typeof(SongCore.Plugin).GetProperty("Configuration", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
                typeof(SongCore.Plugin).Assembly.GetType("SongCore.SConfiguration").GetProperty("CustomSongColors").SetValue(sConfiguration, value);
            }
        }

        internal void ShowColors(DifficultyColors colors)
        {
            Parse();
            _modal.Show(true);
            SetColors(colors);
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
            ColorSchemeView colorSchemeView = GameObject.Instantiate(Resources.FindObjectsOfTypeAll<ColorSchemeView>().First(), selectedColorTransform);
            boostedColorSchemeView = IPA.Utilities.ReflectionUtil.CopyComponent<BoostedColorSchemeView>(colorSchemeView, colorSchemeView.gameObject);
            GameObject.DestroyImmediate(colorSchemeView);
            boostedColorSchemeView.Setup();
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

            boostedColorSchemeView.SetColors(saberLeft, saberRight, envLeft, envRight, envLeftBoost, envRightBoost, obstacle);
        }
    }
}
