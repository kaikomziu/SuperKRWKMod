using BongoCat;
using SuperKRWKMod.Config;
using SuperKRWKMod.Debug;
using SuperKRWKMod.Features;
using SuperKRWKMod.ModList;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SuperKRWKMod
{
	public class SuperKRWKMod : MonoBehaviour
	{
		// Assembly attributes can't be read at runtime: the game's stripped mscorlib lacks their types.
		public const string Version = "0.3.1";

		private const string UiPath = "DraggableFrame/UI";
		private const string VisualsPath = "DraggableFrame/CatVisuals";
		private const string CounterPath = "DraggableFrame/UI/Counter_Frame/Counter";
		private static readonly string[] TintPaths = { "Body", "RightPaw" };

		private readonly ModConfig _config = new ModConfig();
		private TapTracker _tracker;
		private SpeedMeter _meter;

		private void Awake()
		{
			Log.InfoT(this, $"SuperKRWKMod v{Version} loaded.");
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
			var sleep = gameObject.AddComponent<SleepMode>();
			sleep.Init(_config, _tracker, motion, ui, counterStyle);
			var body = cat.transform.Find("Body")?.GetComponent<Image>();
			if (body != null)
				gameObject.AddComponent<SleepyEyes>().Init(_config, sleep, body);
			_meter = gameObject.AddComponent<SpeedMeter>();
			_meter.Init(_config, _tracker, ui, counterStyle);

			Log.InfoT(this, $"Features ready (tint targets: {tintTargets.Count}, counter style: {counterStyle != null}).");

			RegisterInModList();
			var menuTabs = mainCat.root.GetComponentsInChildren<MenuTabs>(true);
			if (menuTabs.Length > 0)
				gameObject.AddComponent<ModsSettingsPage>().Init(menuTabs[0]);
			else
				Log.WarnT(this, "Game menu not found; Mods settings page disabled.");
		}

		private void RegisterInModList()
		{
			var info = new ModInfo
			{
				Name = "SuperKRWKMod",
				Version = Version,
				Author = () => ModText.Get("by KRWK (based on Catnip Mod by Shilo)", "作者: KRWK（ShiloさんのCatnip Modがベース）"),
				Description = () => ModText.Get("Visual-only fun. Never changes taps, stats or items.", "見た目だけの改造です。タップ数・統計・アイテムは一切変更しません。"),
				Hotkeys = () => ModText.Get(
					$"{_config.MeterToggleKey}: show/hide meter   {_config.ReloadKey}: reload superkrwk.ini",
					$"{_config.MeterToggleKey}: メーター表示切替   {_config.ReloadKey}: superkrwk.ini 再読込"),
			};
			info.Toggles.Add(ConfigToggle(() => ModText.Get("Zoomies (hop & rainbow)", "ズーミー（ジャンプ＆虹色）"), "Zoomies"));
			info.Toggles.Add(ConfigToggle(() => ModText.Get("Typing speed meter", "打鍵速度メーター"), "Meter"));
			info.Toggles.Add(ConfigToggle(() => ModText.Get("Sleep mode (Zzz)", "おやすみモード（Zzz）"), "Sleep"));
			info.Toggles.Add(new ModToggle
			{
				Label = () => ModText.Get("Close eyes while asleep", "寝ているときに目を閉じる"),
				Get = () => _config.SleepCloseEyes,
				Set = value => _config.Set("Sleep", "CloseEyes", value ? "true" : "false"),
			});
			ModRegistry.Register(info);
		}

		private ModToggle ConfigToggle(Func<string> label, string section)
		{
			return new ModToggle
			{
				Label = label,
				Get = () => _config.GetBool(section, "Enabled", true),
				Set = value =>
				{
					_config.Set(section, "Enabled", value ? "true" : "false");
					ApplyConfig();
				},
			};
		}

		private void ApplyConfig()
		{
			_tracker.WindowSeconds = _config.WindowSeconds;
			_meter.ApplyConfig();
		}

		private void Update()
		{
			if (_tracker == null || !Input.GetKeyDown(_config.ReloadKey))
				return;

			_config.Load();
			ApplyConfig();
			Log.InfoT(this, "Config reloaded.");
		}

		private void OnDestroy()
		{
			Log.InfoT(this, "Mod unloaded.");
		}
	}
}
