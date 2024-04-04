using System;
using Newtonsoft.Json;

// ReSharper disable InconsistentNaming
namespace MultiplayerCore.Models
{
    [Serializable]
    public class MpStatusData : MultiplayerStatusData
    {
        /// <summary>
        /// Handled by MultiplayerCore. If defined, and if a mod with a bad version is found, the multiplayer status
        /// check fails and MUR-5 is returned.
        /// </summary>
        [JsonProperty("required_mods")]
        public RequiredMod[]? requiredMods { get; set; }

        /// <summary>
        /// Handled by MultiplayerCore. If defined, and if the current game version exceeds this version, the
        /// multiplayer status check fails and MUR-6 is returned.
        /// </summary>
        [JsonProperty("maximum_app_version")]
        public string? maximumAppVersion { get; set; }

        /// <summary>
        /// Information only. Indicates whether dedicated server connections should use SSL/TLS. Currently, most modded
        /// multiplayer servers do not use encryption.
        /// </summary>
        [JsonProperty("use_ssl")]
        public bool useSsl { get; set; }

        /// <summary>
        /// Information only. Master server display name.
        /// </summary>
        [JsonProperty("name")]
        public string? name { get; set; }

        /// <summary>
        /// Information only. Master server display description.
        /// </summary>
        [JsonProperty("description")]
        public string? description { get; set; }

        /// <summary>
        /// Information only. Master server display image URL.
        /// </summary>
        [JsonProperty("image_url")]
        public string? imageUrl { get; set; }

        /// <summary>
        /// Information only. Maximum player count when creating new lobbies.
        /// </summary>
        [JsonProperty("max_players")]
        public int maxPlayers { get; set; }

        /// <summary>
        /// Information only. Server capability: per-player modifiers.
        /// </summary>
        [JsonProperty("supports_pp_modifiers")]
        public bool supportsPPModifiers { get; set; }

        /// <summary>
        /// Information only. Server capability: per-player difficulties.
        /// </summary>
        [JsonProperty("supports_pp_difficulties")]
        public bool supportsPPDifficulties { get; set; }

        /// <summary>
        /// Information only. Server capability: per-player level selection.
        /// </summary>
        [JsonProperty("supports_pp_maps")]
        public bool supportsPPMaps { get; set; }

        [Serializable]
        public class RequiredMod
        {
            /// <summary> 
            /// BSIPA Mod ID.
            /// </summary>
            public string id = null!;

            /// <summary>
            /// Minimum version of the mod required, if installed.
            /// </summary>
            public string version = null!;

            /// <summary>
            /// Indicates whether the mod must be installed.
            /// </summary>
            public bool required = false;
        }
    }
}