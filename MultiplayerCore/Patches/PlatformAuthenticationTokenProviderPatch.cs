using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace MultiplayerCore.Patches
{
	[HarmonyPatch]
	internal class PlatformAuthenticationTokenProviderPatch
	{
		public static readonly string DummyAuth = "77686f5f69735f72656d5f3f";

		[HarmonyPostfix]
		[HarmonyPatch(typeof(PlatformAuthenticationTokenProvider), nameof(PlatformAuthenticationTokenProvider.GetAuthenticationToken))]
		private static void GetAuthenticationToken(PlatformAuthenticationTokenProvider __instance, ref Task<AuthenticationToken> __result)
		{
			__result = __result.ContinueWith<AuthenticationToken>(task =>
			{
				AuthenticationToken result;
				if (task.IsFaulted || string.IsNullOrWhiteSpace((result = task.Result).sessionToken))
				{
					Plugin.Logger.Error("An error occurred while attempting to get the auth token: " + task.Exception);
					Plugin.Logger.Warn("Failed to get auth token, returning custom authentication token!");
					return new AuthenticationToken(__instance.platform, __instance.hashedUserId, __instance.userName, DummyAuth);
				}
				Plugin.Logger.Debug("Successfully got auth token!");
				return result;
			});
		}
	}
}
