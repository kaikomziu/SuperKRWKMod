using BongoCat;
using CatnipMod.Config;
using CatnipMod.Debug;
using CatnipMod.Features;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CatnipMod
{
	public class CatnipMod : MonoBehaviour
	{
		// Assembly attributes can't be read at runtime: the game's stripped mscorlib lacks their types.
		public const string Version = "0.2.0";

		private const string UiPath = "DraggableFrame/UI";
		private const string VisualsPath = "DraggableFrame/CatVisuals";
		private const string CounterPath = "DraggableFrame/UI/Counter_Frame/Counter";
		private static readonly string[] TintPaths = { "Body", "RightPaw" };

		private readonly CatnipConfig _config = new CatnipConfig();
		private TapTracker _tracker;
		private SpeedMeter _meter;

		private void Awake()
		{
			Log.InfoT(this, $"Mod v{Version} by Shilo loaded.");
			_config.Load();
			StartCoroutine(InitWhenReady());
		}

		private IEnumerator InitWhenReady()
		{
			while (Cat.Instance == null || MainCat.Instance == null || OnDemandRenderHelper.Instance == null)
				yield return null;

			// Let the game finish its own Start() calls first.
			yield return null;

			var cat = Cat.Instance;
			var mainCat = MainCat.Instance.transform;
			var ui = mainCat.Find(UiPath);
			var visuals = mainCat.Find(VisualsPath);
			var counterStyle = mainCat.Find(CounterPath)?.GetComponent<TMP_Text>();

			if (ui == null || visuals == null)
			{
				Log.ErrorT(this, $"Cat UI not found (ui={ui != null}, visuals={visuals != null}); the game layout may have changed. Features disabled.");
				yield break;
			}

			if (counterStyle == null)
				Log.WarnT(this, "Counter text not found; labels will use the default font.");

			var tintTargets = new List<Graphic>();
			foreach (var path in TintPaths)
			{
				var graphic = cat.transform.Find(path)?.GetComponent<Graphic>();
				if (graphic != null)
					tintTargets.Add(graphic);
			}

			_tracker = gameObject.AddComponent<TapTracker>();
			_tracker.WindowSeconds = _config.WindowSeconds;
			_tracker.Attach(cat);

			var motion = gameObject.AddComponent<CatMotion>();
			motion.Attach(visuals);

			gameObject.AddComponent<Zoomies>().Init(_config, _tracker, motion, tintTargets.ToArray());
			gameObject.AddComponent<SleepMode>().Init(_config, _tracker, motion, ui, counterStyle);
			_meter = gameObject.AddComponent<SpeedMeter>();
			_meter.Init(_config, _tracker, ui, counterStyle);

			Log.InfoT(this, $"Features ready (tint targets: {tintTargets.Count}, counter style: {counterStyle != null}).");
		}

		private void Update()
		{
			if (_tracker == null || !Input.GetKeyDown(_config.ReloadKey))
				return;

			_config.Load();
			_tracker.WindowSeconds = _config.WindowSeconds;
			_meter.ApplyConfig();
			Log.InfoT(this, "Config reloaded.");
		}

		private void OnDestroy()
		{
			Log.InfoT(this, "Mod unloaded.");
		}
	}
}
