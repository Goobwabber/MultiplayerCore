using BeatSaverSharp;
using HarmonyLib;
using IPA;
using IPA.Config;
using IPA.Loader;
using MultiplayerCore.Installers;
using SiraUtil.Zenject;
using System;
using IPALogger = IPA.Logging.Logger;

namespace MultiplayerCore
{
    [Plugin(RuntimeOptions.SingleStartInit)]
    class Plugin
    {
        private readonly Harmony _harmony;
        private readonly PluginMetadata _metadata;
		private readonly BeatSaver _beatsaver;
        public const string ID = "com.goobwabber.multiplayercore";

		[Init]
		public Plugin(IPALogger logger, PluginMetadata pluginMetadata, Zenjector zenjector)
		{
			_harmony = new Harmony(ID);
			_metadata = pluginMetadata;
			_beatsaver = new BeatSaver(ID, new Version(_metadata.HVersion.ToString()));

			zenjector.UseMetadataBinder<Plugin>();
			zenjector.UseLogger(logger);
			zenjector.UseHttpService();
			zenjector.UseSiraSync(SiraUtil.Web.SiraSync.SiraSyncServiceType.GitHub, "Goobwabber", "MultiplayerCore");
			zenjector.Install<MpAppInstaller>(Location.App, _beatsaver);
			zenjector.Install<MpMenuInstaller>(Location.Menu);
			zenjector.Install<MpGameInstaller>(Location.MultiplayerCore);
		}

		[OnEnable]
		public void OnEnable()
		{
			_harmony.PatchAll(_metadata.Assembly);
		}

		[OnDisable]
		public void OnDisable()
		{
			_harmony.UnpatchAll(ID);
		}
	}
}
