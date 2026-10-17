using Steamworks;
using System;
using System.Runtime.InteropServices;

namespace SuperKRWKMod.Utilities
{
	public static class DoorstopUtil
	{
		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern bool SetEnvironmentVariable(string lpName, string lpValue);

		/// <summary>
		/// Workaround fix for Doorstop bug:
		/// If Steam is not running on direct game launch, Doorstop environment variables never get unset.
		/// After game executable restarts via Steam, the environment variables are still set,
		/// causing Doorstop to skip initialization and SuperKRWKMod never loads.
		/// Without this fix, users would have to restart Steam to load SuperKRWKMod again.
		/// </summary>
		/// <remarks>
		/// Uses Win32 API directly via P/Invoke to work with stripped assemblies.
		/// See: https://github.com/NeighTools/UnityDoorstop/issues/34
		/// </remarks>
		public static void UnsetEnvVarsIfNotSteamRunning()
		{
			if (SteamAPI.IsSteamRunning())
				return;

			SetEnvironmentVariable("DOORSTOP_DISABLE", null);
			SetEnvironmentVariable("DOORSTOP_INITIALIZED", null);
		}
	}
}

