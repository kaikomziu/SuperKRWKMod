using SuperKRWKMod.Debug;
using System;
using System.Globalization;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace SuperKRWKMod.ModList
{
	/// <summary>
	/// Picks mod UI text for the game's selected language (Settings &gt; General &gt; Language), or the
	/// one forced with [General] Language in superkrwk.ini. English and Japanese are written at each
	/// call site; other languages come from <see cref="Translations"/>, keyed by the English text.
	/// English is the fallback for anything without a translation.
	/// </summary>
	public static class ModText
	{
		private static bool _hooked;
		private static Action _languageChanged;
		private static string _override;

		/// <summary>Raised after the language changes (in the game or through the override).</summary>
		public static event Action LanguageChanged
		{
			add
			{
				Hook();
				_languageChanged += value;
			}
			remove
			{
				_languageChanged -= value;
			}
		}

		/// <summary>A language code to use instead of the game's ("auto" or empty = follow the game).</summary>
		public static void SetOverride(string code)
		{
			var value = string.IsNullOrEmpty(code) || string.Equals(code, "auto", StringComparison.OrdinalIgnoreCase) ? null : code.Trim();
			if (value == _override)
				return;

			_override = value;
			Log.Info($"Mod language: {Language}");
			_languageChanged?.Invoke();
		}

		/// <summary>Normalised language of the mod's text (e.g. "en", "ja", "zh-CN", "pt").</summary>
		public static string Language => Normalize(_override ?? LocalizationSettings.SelectedLocale?.Identifier.Code);

		public static bool IsJapanese => Language == "ja";

		public static string Get(string english, string japanese)
		{
			var language = Language;
			if (language == "ja")
				return japanese;
			if (language == "en")
				return english;
			return Translations.Lookup(language, english) ?? english;
		}

		/// <summary><see cref="Get"/> for a template with {0}, {1}... placeholders.</summary>
		public static string Format(string english, string japanese, params object[] args)
		{
			return string.Format(CultureInfo.InvariantCulture, Get(english, japanese), args);
		}

		private static string Normalize(string code)
		{
			if (string.IsNullOrEmpty(code))
				return "en";

			var c = code.Trim().ToLowerInvariant().Replace('_', '-');
			if (c.StartsWith("zh"))
				return c.Contains("tw") || c.Contains("hk") || c.Contains("hant") ? "zh-TW" : "zh-CN";
			if (c == "nb" || c == "nn" || c.StartsWith("no"))
				return "no";

			// One translation per language: regional variants (fr-CA, es-419, pt-PT...) share it.
			int dash = c.IndexOf('-');
			return dash > 0 ? c.Substring(0, dash) : c;
		}

		private static void Hook()
		{
			if (_hooked)
				return;

			_hooked = true;
			LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
		}

		private static void OnLocaleChanged(Locale locale)
		{
			Log.Info($"Language changed to {locale?.Identifier.Code}");
			if (_override == null)
				_languageChanged?.Invoke();
		}
	}
}
