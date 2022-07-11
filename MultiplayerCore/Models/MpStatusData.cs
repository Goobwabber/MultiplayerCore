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

        public RequiredMod[] requiredMods = null!;

        [Serializable]
        public class RequiredMod
        {
            public string id = null!;
            public string version = null!;
        }
    }
}
