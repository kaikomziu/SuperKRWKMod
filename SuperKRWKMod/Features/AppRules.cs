using SuperKRWKMod.Config;
using SuperKRWKMod.Debug;
using SuperKRWKMod.ModList;
using SuperKRWKMod.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SuperKRWKMod.Features
{
	/// <summary>
	/// [AppRules] in superkrwk.ini: while a listed app is running, the given features are switched off,
	/// and they come back on their own when it closes. Rules can be edited in game (Settings &gt; Mods);
	/// the suppression itself is in memory only and never rewrites the feature settings.
	/// </summary>
	public class AppRules : MonoBehaviour
	{
		public const string Section = "AppRules";
		private const float PollSeconds = 2f;

		/// <summary>Feature names usable in rules, with their display names.</summary>
		private static readonly string[][] FeatureNames =
		{
			new[] { ModConfig.FeatureAll, "Everything", "すべて" },
			new[] { ModConfig.FeatureZoomies, "Zoomies", "ズーミー" },
			new[] { ModConfig.FeatureRainbow, "Rainbow glow", "虹色" },
			new[] { ModConfig.FeatureMeter, "Speed meter", "メーター" },
			new[] { ModConfig.FeatureCombo, "Combo", "コンボ" },
			new[] { ModConfig.FeatureSleep, "Sleep mode", "おやすみ" },
			new[] { ModConfig.FeatureEyes, "Eye movement", "目の動き" },
			new[] { ModConfig.FeatureChest, "Chest notice", "宝箱の通知" },
		};

		private class Rule
		{
			public string App;
			public List<string> Features;
		}

		private ModConfig _config;
		private readonly List<Rule> _rules = new List<Rule>();
		private readonly HashSet<string> _activeApps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		private float _nextPoll;
		private bool _picking;

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
				var app = NormalizeApp(pair.Key);
				if (app.Length == 0)
					continue;

				var features = new List<string>();
				foreach (var part in pair.Value.Split(','))
				{
					var feature = part.Trim().ToLowerInvariant();
					if (IsKnown(feature))
					{
						if (!features.Contains(feature))
							features.Add(feature);
					}
					else if (feature.Length > 0)
					{
						Log.WarnT(this, $"Unknown feature '{feature}' in rule for {app}.");
					}
				}

				// Rules with no features are kept so they can be edited in game.
				_rules.Add(new Rule { App = app, Features = features });
			}

			_rules.Sort((a, b) => string.Compare(a.App, b.App, StringComparison.OrdinalIgnoreCase));
			Log.InfoT(this, $"{_rules.Count} app rule(s).");
			_nextPoll = 0f;
		}

		private static string NormalizeApp(string name)
		{
			var app = name.Trim().Trim('"');
			if (app.Length > 0 && !app.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
				app += ".exe";
			return app;
		}

		// --- Polling -------------------------------------------------------------------------------

		private void Update()
		{
			if (Time.unscaledTime < _nextPoll)
				return;
			_nextPoll = Time.unscaledTime + PollSeconds;
			Evaluate();
		}

		private void Evaluate()
		{
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
					Log.InfoT(this, $"{app} started; switching off: {string.Join(", ", Find(app)?.Features.ToArray() ?? new string[0])}.");
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

		// --- Editing -------------------------------------------------------------------------------

		private Rule Find(string app)
		{
			foreach (var rule in _rules)
				if (string.Equals(rule.App, app, StringComparison.OrdinalIgnoreCase))
					return rule;
			return null;
		}

		private void Save(Rule rule)
		{
			_config.Set(Section, rule.App, string.Join(", ", rule.Features.ToArray()));
			Evaluate();
		}

		private void AddRule(string app)
		{
			app = NormalizeApp(app);
			if (app.Length == 0 || Find(app) != null)
				return;

			var rule = new Rule { App = app, Features = new List<string> { ModConfig.FeatureAll } };
			_rules.Add(rule);
			_rules.Sort((a, b) => string.Compare(a.App, b.App, StringComparison.OrdinalIgnoreCase));
			Log.InfoT(this, $"Added rule for {app}.");
			Save(rule);
		}

		private void RemoveRule(Rule rule)
		{
			_rules.Remove(rule);
			_config.Remove(Section, rule.App);
			Log.InfoT(this, $"Removed rule for {rule.App}.");
			Evaluate();
		}

		private void SetFeature(Rule rule, string feature, bool off)
		{
			if (off && !rule.Features.Contains(feature))
				rule.Features.Add(feature);
			else if (!off)
				rule.Features.Remove(feature);
			Save(rule);
		}

		// --- Mods page -----------------------------------------------------------------------------

		/// <summary>Builds the rule editor on the Mods settings page.</summary>
		public void Build(IModPageBuilder page)
		{
			page.Divider();
			if (_picking)
				BuildPicker(page);
			else
				BuildList(page);
		}

		private void BuildList(IModPageBuilder page)
		{
			page.Text("<b>" + ModText.Get("App rules", "アプリ別ルール") + "</b>", 0.9f);
			page.Text(ModText.Get(
				"While an app below is running, the checked features are switched off. They come back when it closes.",
				"下のアプリが起動している間だけ、チェックした機能をオフにします。アプリを閉じると元に戻ります。"), 0.7f);

			foreach (var rule in _rules)
			{
				var r = rule;
				bool running = _activeApps.Contains(r.App);
				page.Text((running ? "<color=#3a9a4a>●</color> " : "○ ") + "<b>" + DisplayApp(r.App) + "</b>"
					+ (running ? ModText.Get(" (running now)", "（起動中）") : ""), 0.85f, true);

				foreach (var names in FeatureNames)
				{
					var feature = names[0];
					page.Toggle(new ModToggle
					{
						Label = () => ModText.Format("{0} off", "{0}をオフ", ModText.Get(names[1], names[2])),
						Get = () => r.Features.Contains(feature),
						Set = value => SetFeature(r, feature, value),
					});
				}

				page.Button(ModText.Get("Delete this rule", "このルールを削除"), () =>
				{
					RemoveRule(r);
					page.Refresh();
				});
			}

			if (_rules.Count == 0)
				page.Text(ModText.Get("(no rules yet)", "（まだルールはありません）"), 0.75f);

			page.Button(ModText.Get("+ Add from running apps", "＋ 起動中のアプリから追加"), () =>
			{
				_picking = true;
				page.Refresh();
			});
		}

		private void BuildPicker(IModPageBuilder page)
		{
			page.Text("<b>" + ModText.Get("Choose an app", "追加するアプリを選択") + "</b>", 0.9f);
			page.Text(ModText.Get(
				"Apps with an open window. Start the app first if it isn't listed.",
				"今ウィンドウを開いているアプリです。見つからないときは先にそのアプリを起動してください。"), 0.7f);

			List<string> apps;
			try
			{
				apps = ProcessList.WindowedApps();
			}
			catch (Exception e)
			{
				Log.ExceptionT(this, e, "Could not list open apps");
				apps = new List<string>();
			}

			int shown = 0;
			foreach (var app in apps)
			{
				if (Find(app) != null)
					continue;

				var a = app;
				page.Button(DisplayApp(a), () =>
				{
					AddRule(a);
					_picking = false;
					page.Refresh();
				});
				shown++;
			}

			if (shown == 0)
				page.Text(ModText.Get("(no other open apps)", "（追加できるアプリがありません）"), 0.75f);

			page.Button(ModText.Get("Cancel", "キャンセル"), () =>
			{
				_picking = false;
				page.Refresh();
			});
		}

		private static string DisplayApp(string app)
		{
			return app.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? app.Substring(0, app.Length - 4) : app;
		}

		private static bool IsKnown(string feature)
		{
			foreach (var names in FeatureNames)
				if (names[0] == feature)
					return true;
			return false;
		}
	}
}
