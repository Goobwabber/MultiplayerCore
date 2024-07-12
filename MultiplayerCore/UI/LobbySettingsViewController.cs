using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.ViewControllers;
using IPA.Utilities;
using Zenject;

namespace MultiplayerCore.UI
{
    [ViewDefinition("MultiplayerCore.UI.LobbySettingsViewController.bsml")]
    public class LobbySettingsViewController : BSMLAutomaticViewController
    {
        private FieldAccessor<GameplaySetupViewController, bool>.Accessor _showModifiers
            = FieldAccessor<GameplaySetupViewController, bool>.GetAccessor(nameof(_showModifiers));
        private FieldAccessor<GameplaySetupViewController, bool>.Accessor _showEnvironmentOverrideSettings
            = FieldAccessor<GameplaySetupViewController, bool>.GetAccessor(nameof(_showEnvironmentOverrideSettings));
        private FieldAccessor<GameplaySetupViewController, bool>.Accessor _showColorSchemesSettings
            = FieldAccessor<GameplaySetupViewController, bool>.GetAccessor(nameof(_showColorSchemesSettings));
        private FieldAccessor<GameplaySetupViewController, bool>.Accessor _showMultiplayer
            = FieldAccessor<GameplaySetupViewController, bool>.GetAccessor(nameof(_showMultiplayer));

        private GameplaySetupViewController _gameplaySetup = null!;
        private bool _perPlayerDiffs = false;
        private bool _perPlayerModifiers = false;
        //private Config _config = null!;

        [Inject]
        private void Construct(
            GameplaySetupViewController gameplaySetup/*,
            Config config*/)
		{
            _gameplaySetup = gameplaySetup;
            //_config = config;
        }

        [UIAction("#post-parse")]
        private void PostParse()
        {
            //_sideBySideDistanceIncrement.interactable = _sideBySide;
        }

        [UIValue("per-player-diffs")]
        public bool PerPlayerDifficulty
		{
			//get => _config.SoloEnvironment;
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
    }
}
