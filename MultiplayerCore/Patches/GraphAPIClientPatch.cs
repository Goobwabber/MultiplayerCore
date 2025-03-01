using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BGNet.Core.GameLift;
using HarmonyLib;
using SiraUtil.Affinity;

namespace MultiplayerCore.Patches
{
	[HarmonyPatch]
	internal class GraphAPIClientPatch
	{
		private static readonly HttpClient _client = new HttpClient();

		private static readonly MethodInfo _sendAsyncAttacher = SymbolExtensions.GetMethodInfo(() => SendAsync(null!, 0, CancellationToken.None));
		private static readonly MethodInfo _sendAsyncMethod = typeof(HttpClient).GetMethod(nameof(HttpClient.SendAsync), new Type[] { typeof(HttpRequestMessage), typeof(HttpCompletionOption), typeof(CancellationToken) })!;

		static MethodBase TargetMethod() =>
			AccessTools.FirstInner(typeof(GraphAPIClient), t => t.Name.StartsWith("<Post>d__5`1"))?.MakeGenericType(typeof(GetMultiplayerInstanceResponse)).GetMethod("MoveNext", BindingFlags.NonPublic | BindingFlags.Instance)!;

		private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			Plugin.Logger.Trace("Transpiling GraphAPIClient.Post");
			var codes = instructions.ToList();
			for (int i = 0; i < codes.Count; i++)
			{
				Plugin.Logger.Trace($"Instruction at index {i}: {codes[i].opcode}");
				if (codes[i].opcode == OpCodes.Callvirt)
				{
					Plugin.Logger.Trace($"Callvirt Method: {codes[i].operand}");
					if (codes[i].Calls(_sendAsyncMethod))
					{
						Plugin.Logger.Trace("Found SendAsync call");
						CodeInstruction newCode = new CodeInstruction(OpCodes.Callvirt, _sendAsyncAttacher);
						codes[i] = newCode;
					}
				}
			}

			return codes.AsEnumerable();
		}

		private static Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpCompletionOption completionOption,
			CancellationToken cancellationToken)
		{

			Plugin.Logger.Debug("SendAsync MasterServer Request called");
			var result = _client.SendAsync(request, completionOption, cancellationToken);

			result.ContinueWith(async task =>
			{
				try
				{
					HttpResponseMessage response = await task;
					if (!response.IsSuccessStatusCode)
					{
						Plugin.Logger.Error(
							$"An error occurred while attempting to post to the Graph API: Uri '{request.RequestUri}' StatusCode: {(int)response.StatusCode}: {response.StatusCode}");
						Plugin.Logger.Trace($"Response: {response.Content.ReadAsStringAsync()}");
					}

				}
				catch (Exception ex)
				{
					Plugin.Logger.Error(
						$"An error occurred while attempting to post to the Graph API: Uri '{request.RequestUri}' Exception Message: {ex.Message + (ex.InnerException != null ? " --> " + ex.InnerException.Message : "")}");
					Plugin.Logger.Trace(ex);
				}
			});
			return result;
		}
	}
}
