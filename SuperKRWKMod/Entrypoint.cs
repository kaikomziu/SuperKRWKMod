using SuperKRWKMod.Debug;
using SuperKRWKMod.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Doorstop
{
	internal class Entrypoint
	{
		private static bool _modCreated = false;

		public static void Start()
		{
			Log.SetupTimestampHandler();
			DoorstopUtil.UnsetEnvVarsIfNotSteamRunning();

			// Harmony can't load in this game: its stripped System.dll lacks ConcurrentBag<T>,
			// which MonoMod needs. Bootstrap from the first loaded scene instead.
			SceneManager.sceneLoaded += OnSceneLoaded;
		}

		private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			if (_modCreated)
				return;

			_modCreated = true;
			SceneManager.sceneLoaded -= OnSceneLoaded;

			var mod = new GameObject(nameof(SuperKRWKMod.SuperKRWKMod));
			mod.AddComponent<SuperKRWKMod.SuperKRWKMod>();
			Object.DontDestroyOnLoad(mod);
		}
	}
}
