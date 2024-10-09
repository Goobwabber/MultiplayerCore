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
        public RequiredMod[]? requiredMods 
        {
            get => required_mods;
            set => required_mods = value;
        }
        public RequiredMod[]? required_mods; 

        /// <summary>
        /// Handled by MultiplayerCore. If defined, and if the current game version exceeds this version, the
        /// multiplayer status check fails and MUR-6 is returned.
        /// </summary>
        public string? maximumAppVersion 
        {
            get => maximum_app_version;
            set => maximum_app_version = value;
        }
        public string? maximum_app_version;

        /// <summary>
        /// Information only. Indicates whether dedicated server connections should use SSL/TLS. Currently, most modded
        /// multiplayer servers do not use encryption.
        /// </summary>
        public bool useSsl
        {
            get => use_ssl;
            set => use_ssl = value;
        }
        public bool use_ssl;

        /// <summary>
        /// Information only. Master server display name.
        /// </summary>
        public string? name { get; set; }

        /// <summary>
        /// Information only. Master server display description.
        /// </summary>
        public string? description { get; set; }

        /// <summary>
        /// Information only. Master server display image URL.
        /// </summary>
        public string? imageUrl
        {
            get => image_url;
            set => image_url = value;
        }
        public string? image_url;

        /// <summary>
        /// Information only. Maximum player count when creating new lobbies.
        /// </summary>
        public int maxPlayers
        {
            get => max_players;
            set => max_players = value;
        }
        public int max_players;

		/// <summary>
		/// Information only. Server capability: per-player modifiers.
		/// </summary>
        public bool supportsPPModifiers
        {
            get => supports_pp_modifiers;
            set => supports_pp_modifiers = value;
		}
        public bool supports_pp_modifiers;

		/// <summary>
		/// Information only. Server capability: per-player difficulties.
		/// </summary>
        public bool supportsPPDifficulties
        {
            get => supports_pp_difficulties;
            set => supports_pp_difficulties = value;
		}
        public bool supports_pp_difficulties;

		/// <summary>
		/// Information only. Server capability: per-player level selection.
		/// </summary>
        public bool supportsPPMaps
        {
            get => supports_pp_maps;
            set => supports_pp_maps = value;
		}
        public bool supports_pp_maps;

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