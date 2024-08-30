using System;
using System.Collections.Generic;
using MultiplayerCore.Models;
using SiraUtil.Affinity;
using SiraUtil.Logging;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable InconsistentNaming

namespace MultiplayerCore.Repositories
{
    /// <summary>
    /// Provides multiplayer status data for all master servers contacted during the game session.
    /// </summary>
    public class MpStatusRepository : IAffinity
    {
        private readonly INetworkConfig _networkConfig;
        private readonly SiraLog _logger;
        
        private readonly Dictionary<string, MpStatusData> _statusByUrl;
        private readonly Dictionary<string, MpStatusData> _statusByHostname;
        
        public event Action<string, MpStatusData>? statusUpdatedForUrlEvent;

        internal MpStatusRepository(INetworkConfig networkConfig, SiraLog logger)
        {
            _networkConfig = networkConfig;
            _logger = logger;
            
            _statusByUrl = new();
            _statusByHostname = new();
        }

        #region API
        
        internal void ReportStatus(MpStatusData statusData)
        {
            var statusUrl =  _networkConfig.multiplayerStatusUrl;
            _statusByUrl[statusUrl] = statusData;
            RaiseUpdateEvent(statusUrl, statusData);
        }

        /// <summary>
        /// Retrieve the latest multiplayer status data for a given Status URL.
        /// </summary>
        public MpStatusData? GetStatusForUrl(string statusUrl)
            => _statusByUrl.TryGetValue(statusUrl, out var statusData) ? statusData : null;
        
        #endregion

        #region Events

        private void RaiseUpdateEvent(string url, MpStatusData statusData)
        {
            try
            {
                statusUpdatedForUrlEvent?.Invoke(url, statusData);
            }
            catch (Exception ex)
            {
                _logger.Error("Error in statusUpdatedForUrlEvent handler:");
                _logger.Error(ex);
            }
        }

        #endregion
        
        #region Patch

        [AffinityPrefix]
        [AffinityPatch(typeof(MultiplayerUnavailableReasonMethods),
            nameof(MultiplayerUnavailableReasonMethods.TryGetMultiplayerUnavailableReason))]
        private void PrefixTryGetMultiplayerUnavailableReason(MultiplayerStatusData data)
        {
            // TryGetMultiplayerUnavailableReason is called whenever a server response is parsed

            // MultiplayerStatusModelPatch should have "upgraded" this to an instance of MultiplayerStatusData 
            if (data is MpStatusData mpStatusData)
                ReportStatus(mpStatusData);
        }
        
        #endregion
    }
}