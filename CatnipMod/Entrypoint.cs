using CatnipMod.Debug;
using CatnipMod.Utilities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Doorstop
{
	internal class Entrypoint
	{
		private static bool _catnipModCreated = false;

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
			if (_catnipModCreated)
				return;

			_catnipModCreated = true;
			SceneManager.sceneLoaded -= OnSceneLoaded;

			var catnipMod = new GameObject(nameof(CatnipMod.CatnipMod));
			catnipMod.AddComponent<CatnipMod.CatnipMod>();
			Object.DontDestroyOnLoad(catnipMod);
		}
	}
}
