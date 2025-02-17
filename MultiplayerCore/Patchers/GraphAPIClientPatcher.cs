using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SiraUtil.Affinity;

namespace MultiplayerCore.Patchers
{
	internal class GraphAPIClientPatcher : IAffinity
	{
		private readonly NetworkConfigPatcher _networkConfig;

		internal GraphAPIClientPatcher(NetworkConfigPatcher networkConfig)
		{
			_networkConfig = networkConfig;
		}

		[AffinityPostfix]
		[AffinityPatch(typeof(HttpClient), nameof(HttpClient.SendAsync), AffinityMethodType.Normal, null, new Type[] { typeof(HttpRequestMessage), typeof(HttpCompletionOption), typeof(CancellationToken) })]
		private async void PostErrorLogger(HttpClient __instance, HttpRequestMessage request, Task<HttpResponseMessage> __result)
		{
			if (!string.IsNullOrWhiteSpace(_networkConfig.GraphUrl) && (request.RequestUri.Host == new Uri(_networkConfig.GraphUrl!).Host || request.RequestUri.OriginalString.StartsWith(_networkConfig.GraphUrl!) || !string.IsNullOrWhiteSpace(_networkConfig.MasterServerStatusUrl) && request.RequestUri.OriginalString.StartsWith(_networkConfig.MasterServerStatusUrl!)))
			{
				try
				{
					HttpResponseMessage response = await __result;
					if (!response.IsSuccessStatusCode)
					{
						Plugin.Logger.Error(
							$"An error occurred while attempting to post to the Graph API: Uri '{request.RequestUri}' StatusCode: {(int)response.StatusCode}: {response.StatusCode}");
						Plugin.Logger.Trace($"Response: {await response.Content.ReadAsStringAsync()}");
					}

				}
				catch (Exception ex)
				{
					Plugin.Logger.Error(
						$"An error occurred while attempting to post to the Graph API: Uri '{request.RequestUri}' Exception Message: {ex.Message + (ex.InnerException != null ? " --> " + ex.InnerException.Message : "")}");
					Plugin.Logger.Trace(ex);
				}
			}
		}

	}
}
