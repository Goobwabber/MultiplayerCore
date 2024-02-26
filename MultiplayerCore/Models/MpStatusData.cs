using Newtonsoft.Json;
using System;

namespace MultiplayerCore.Models
{
    [Serializable]
    public class MpStatusData : MultiplayerStatusData
    {
        [JsonProperty("required_mods")]
        private RequiredMod[] _requiredMods
        {
            get
            {
                return requiredMods;
            }

            set
            {
                requiredMods = value;
            }
        }

        [JsonProperty("maximum_app_version")]
        private string _maximumAppVersion
        {
            get
            {
                return maximumAppVersion;
            }

            set
            {
                maximumAppVersion = value;
            }
        }
        
        [JsonProperty("use_ssl")]
        private bool _useSsl
        {
            get
            {
                return useSsl;
            }

            set
            {
                useSsl = value;
            }
        }

        public RequiredMod[] requiredMods = null!;
        public string maximumAppVersion = null!;
        /// <summary>
        /// Request SSL (DTLS) connections when connecting to dedicated servers.
        /// MultiplayerCore does NOT enforce this setting.
        /// </summary>
        public bool useSsl = false;

        [Serializable]
        public class RequiredMod
        {
            public string id = null!;
            public string version = null!;
        }
    }
}
