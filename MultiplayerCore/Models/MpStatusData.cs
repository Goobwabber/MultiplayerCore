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

        public RequiredMod[] requiredMods = null!;
        public string maximumAppVersion = null!;

        [Serializable]
        public class RequiredMod
        {
            public string id = null!;
            public string version = null!;
        }
    }
}
