using BeatSaverSharp;
using HarmonyLib;
using IPA;
using IPA.Config;
using IPA.Loader;
using MultiplayerCore.Installers;
using SiraUtil.Zenject;
using System;
using System.IO;
using UnityEngine;
using IPALogger = IPA.Logging.Logger;

namespace MultiplayerCore
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    class Plugin
    {
        public const string ID = "com.goobwabber.multiplayercore";
        public const string CustomLevelsPath = "CustomMultiplayerLevels";

        internal static IPALogger Logger = null!;

        private readonly Harmony _harmony;
        private readonly PluginMetadata _metadata;
        private readonly BeatSaver _beatsaver;

        [Init]
        public Plugin(IPALogger logger, PluginMetadata pluginMetadata, Zenjector zenjector)
        {
            _harmony = new Harmony(ID);
            _metadata = pluginMetadata;
            _beatsaver = new BeatSaver(ID, new Version(_metadata.HVersion.ToString()));
            Logger = logger;

            zenjector.UseMetadataBinder<Plugin>();
            zenjector.UseLogger(logger);
            zenjector.UseHttpService();
            zenjector.UseSiraSync(SiraUtil.Web.SiraSync.SiraSyncServiceType.GitHub, "Goobwabber", "MultiplayerCore");
            zenjector.Install<MpAppInstaller>(Location.App, _beatsaver);
            zenjector.Install<MpMenuInstaller>(Location.Menu);
        }

        [OnEnable]
        public void OnEnable()
        {
            SongCore.Collections.AddSeperateSongFolder("Multiplayer", Path.Combine(Application.dataPath, CustomLevelsPath), SongCore.Data.FolderLevelPack.CustomLevels);
            _harmony.PatchAll(_metadata.Assembly);
        }

        [OnDisable]
        public void OnDisable()
        {
            _harmony.UnpatchSelf();
        }
    }
}
