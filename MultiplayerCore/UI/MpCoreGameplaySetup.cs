using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using HMUI;
using SiraUtil.Logging;
using System;
using System.Reflection;
using UnityEngine;
using Zenject;

namespace MultiplayerCore.UI
{
    public class MpCoreGameplaySetup : NotifiableBase, IInitializable, IDisposable
    {
        public const string ResourcePath = "MultiplayerCore.UI.MpCoreGameplaySetup.bsml";

        private GameplaySetupViewController _gameplaySetup;
        private MultiplayerSettingsPanelController _multiplayerSettingsPanel;
        private MainFlowCoordinator _mainFlowCoordinator;
        private MpCoreSetupFlowCoordinator _setupFlowCoordinator;
        //private readonly Config _config;
        private SiraLog _logger;
        private bool _perPlayerDiffs = false;
        private bool _perPlayerModifiers = false;

		internal MpCoreGameplaySetup(
            GameplaySetupViewController gameplaySetup,
            MainFlowCoordinator mainFlowCoordinator,
            MpCoreSetupFlowCoordinator setupFlowCoordinator,
            //Config config,
            SiraLog logger)
        {
            _gameplaySetup = gameplaySetup;
            _multiplayerSettingsPanel = gameplaySetup._multiplayerSettingsPanelController;
            _mainFlowCoordinator = mainFlowCoordinator;
            _setupFlowCoordinator = setupFlowCoordinator;
            //_config = config;
            _logger = logger;
        }

        public void Initialize()
        {
            BSMLParser.instance.Parse(BeatSaberMarkupLanguage.Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), ResourcePath), _multiplayerSettingsPanel.gameObject, this);
            while (0 < _vert.transform.childCount)
                _vert.transform.GetChild(0).SetParent(_multiplayerSettingsPanel.transform);
        }

        public void Dispose()
        {
            
        }

        [UIAction("#post-parse")]
        private void PostParse()
        {
	        
        }


		[UIAction("preferences-click")]
        private void PresentPreferences()
        {
            FlowCoordinator deepestChildFlowCoordinator = DeepestChildFlowCoordinator(_mainFlowCoordinator);
            _setupFlowCoordinator.parentFlowCoordinator = deepestChildFlowCoordinator;
            deepestChildFlowCoordinator.PresentFlowCoordinator(_setupFlowCoordinator);
        }

        private FlowCoordinator DeepestChildFlowCoordinator(FlowCoordinator root)
        {
            var flow = root.childFlowCoordinator;
            if (flow == null) return root;
            if (flow.childFlowCoordinator == null || flow.childFlowCoordinator == flow)
            {
                return flow;
            }
            return DeepestChildFlowCoordinator(flow);
        }

        [UIObject("vert")]
        private GameObject _vert = null!;

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
    }
}
