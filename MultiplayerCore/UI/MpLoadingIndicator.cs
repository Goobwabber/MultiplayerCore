using System;
using System.Linq;
using System.Reflection;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using MultiplayerCore.Objects;
using UnityEngine;
using Zenject;

namespace MultiplayerCore.UI
{
    internal class MpLoadingIndicator : NotifiableBase, IInitializable, IDisposable, ITickable, IProgress<double>
    {
        public const string ResourcePath = "MultiplayerCore.UI.LoadingIndicator.bsml";

        private readonly IMultiplayerSessionManager _sessionManager;
        private readonly ILobbyGameStateController _gameStateController;
        private readonly ILobbyPlayersDataModel _playersDataModel;
        private readonly MpEntitlementChecker _entitlementChecker;
        private readonly MpLevelLoader _levelLoader;
        private readonly CenterStageScreenController _screenController;

        private LoadingControl _loadingControl = null!;
        private bool _isDownloading;

        internal MpLoadingIndicator(
            IMultiplayerSessionManager sessionManager,
            ILobbyGameStateController gameStateController,
            ILobbyPlayersDataModel playersDataModel,
            NetworkPlayerEntitlementChecker entitlementChecker,
            MpLevelLoader levelLoader,
            CenterStageScreenController screenController)
        {
            _sessionManager = sessionManager;
            _gameStateController = gameStateController;
            _playersDataModel = playersDataModel;
            _entitlementChecker = (entitlementChecker as MpEntitlementChecker)!;
            _levelLoader = levelLoader;
            _screenController = screenController;
        }

        public void Initialize()
        {
            BSMLParser.Instance.Parse(BeatSaberMarkupLanguage.Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), ResourcePath), _screenController.gameObject, this);
            GameObject existingLoadingControl = Resources.FindObjectsOfTypeAll<LoadingControl>().First().gameObject;
            GameObject loadingControlGO = GameObject.Instantiate(existingLoadingControl, _vert.transform);
            _loadingControl = loadingControlGO.GetComponent<LoadingControl>();
            _loadingControl.Hide();

            _levelLoader.progressUpdated += Report;
        }

        public void Dispose()
        {
            _levelLoader.progressUpdated -= Report;
        }

        public void Tick()
        {
            if (_isDownloading)
                return;
            
            if (_screenController.countdownShown && _sessionManager.syncTime >= _gameStateController.startTime && _gameStateController.levelStartInitiated && _levelLoader.CurrentLoadingData != null)
                _loadingControl.ShowLoading($"{_playersDataModel.Count(x => _entitlementChecker.GetKnownEntitlement(x.Key, _levelLoader.CurrentLoadingData.beatmapKey.levelId) == EntitlementsStatus.Ok) + 1} of {_playersDataModel.Count - 1} players ready...");
            else
                _loadingControl.Hide();
        }

        public void Report(double value)
        {
            _isDownloading = value < 1.0;
            _loadingControl.ShowDownloadingProgress($"Downloading ({value * 100:F2}%)...", (float)value);
        }

        [UIObject("vert")]
        private GameObject _vert = null!;
    }
}
