using SuperKRWKMod.Config;
using SuperKRWKMod.Debug;
using SuperKRWKMod.ModList;
using SuperKRWKMod.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SuperKRWKMod.Features
{
	/// <summary>
	/// [AppRules] in superkrwk.ini: while a listed app is running, the given features are switched off,
	/// and they come back on their own when it closes. Settings in the ini are never rewritten.
	/// </summary>
	public class AppRules : MonoBehaviour
	{
		public const string Section = "AppRules";
		private const float PollSeconds = 2f;

		/// <summary>Feature names usable in rules, with their display names.</summary>
		private static readonly string[][] FeatureNames =
		{
			new[] { ModConfig.FeatureAll, "everything", "すべて" },
			new[] { ModConfig.FeatureZoomies, "zoomies", "ズーミー" },
			new[] { ModConfig.FeatureRainbow, "rainbow", "虹色" },
			new[] { ModConfig.FeatureMeter, "meter", "メーター" },
			new[] { ModConfig.FeatureCombo, "combo", "コンボ" },
			new[] { ModConfig.FeatureSleep, "sleep", "おやすみ" },
			new[] { ModConfig.FeatureEyes, "eyes", "目の動き" },
		};

		private struct Rule
		{
			public string App;
			public string[] Features;
		}

		private ModConfig _config;
		private readonly List<Rule> _rules = new List<Rule>();
		private readonly HashSet<string> _activeApps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		private float _nextPoll;

		/// <summary>Raised when the set of switched-off features changes.</summary>
		public event Action Changed;

		public void Init(ModConfig config)
		{
			_config = config;
			Reload();
		}

		/// <summary>Re-reads the rules (after the ini is reloaded) and re-checks running apps.</summary>
		public void Reload()
		{
			_rules.Clear();
			foreach (var pair in _config.GetSection(Section))
			{
				var app = pair.Key.Trim().Trim('"');
				if (app.Length == 0)
					continue;
				if (!app.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
					app += ".exe";

				var features = new List<string>();
				foreach (var part in pair.Value.Split(','))
				{
					var feature = part.Trim().ToLowerInvariant();
					if (IsKnown(feature))
						features.Add(feature);
					else if (feature.Length > 0)
						Log.WarnT(this, $"Unknown feature '{feature}' in rule for {app}.");
				}

				if (features.Count > 0)
					_rules.Add(new Rule { App = app, Features = features.ToArray() });
			}

			Log.InfoT(this, $"{_rules.Count} app rule(s).");
			_nextPoll = 0f;
		}

		private void Update()
		{
			if (Time.unscaledTime < _nextPoll)
				return;
			_nextPoll = Time.unscaledTime + PollSeconds;

			var running = _rules.Count > 0 ? SafeRunning() : new HashSet<string>();
			var suppressed = new HashSet<string>();
			var active = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var rule in _rules)
			{
				if (!running.Contains(rule.App))
					continue;
				active.Add(rule.App);
				foreach (var feature in rule.Features)
					suppressed.Add(feature);
			}

			if (active.SetEquals(_activeApps) && suppressed.SetEquals(_config.Suppressed))
				return;

			foreach (var app in active)
				if (!_activeApps.Contains(app))
					Log.InfoT(this, $"{app} started; switching off: {string.Join(", ", RuleFor(app).Features)}.");
			foreach (var app in _activeApps)
				if (!active.Contains(app))
					Log.InfoT(this, $"{app} closed; restoring its features.");

			_activeApps.Clear();
			_activeApps.UnionWith(active);
			_config.SetSuppressed(suppressed);
			Changed?.Invoke();
		}

		private HashSet<string> SafeRunning()
		{
			try
			{
				return ProcessList.RunningExecutables();
			}
			catch (Exception e)
			{
				Log.ExceptionT(this, e, "Could not list running apps");
				_rules.Clear(); // don't keep failing every poll
				return new HashSet<string>();
			}
		}

		private Rule RuleFor(string app)
		{
			foreach (var rule in _rules)
				if (string.Equals(rule.App, app, StringComparison.OrdinalIgnoreCase))
					return rule;
			return default;
		}

		private static bool IsKnown(string feature)
		{
			foreach (var names in FeatureNames)
				if (names[0] == feature)
					return true;
			return false;
		}

		private static string DisplayName(string feature)
		{
			foreach (var names in FeatureNames)
				if (names[0] == feature)
					return ModText.Get(names[1], names[2]);
			return feature;
		}

		/// <summary>Text for the Mods settings page.</summary>
		public string Describe()
		{
			var sb = new StringBuilder();
			sb.Append("<b>").Append(ModText.Get("App rules", "アプリ別ルール")).Append("</b>\n");
			sb.Append(ModText.Get(
				"While an app below is running, its features are switched off. Edit [AppRules] in superkrwk.ini.",
				"下のアプリが起動している間だけ、指定した機能をオフにします。superkrwk.ini の [AppRules] で編集できます。"));

			if (_rules.Count == 0)
			{
				sb.Append('\n').Append(ModText.Get("(no rules)", "（ルールなし）"));
				return sb.ToString();
			}

			foreach (var rule in _rules)
			{
				var names = new List<string>();
				foreach (var feature in rule.Features)
					names.Add(DisplayName(feature));

				bool on = _activeApps.Contains(rule.App);
				sb.Append('\n').Append(on ? "<color=#3a9a4a>●</color> " : "○ ")
					.Append(rule.App.Substring(0, rule.App.Length - 4)).Append(": ")
					.Append(string.Join(ModText.Get(", ", "・"), names.ToArray()))
					.Append(ModText.Get(" off", " オフ"))
					.Append(on ? ModText.Get(" (running now)", "（起動中）") : "");
			}

			return sb.ToString();
		}
	}
}
