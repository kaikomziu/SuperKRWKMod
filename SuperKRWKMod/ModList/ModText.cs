using SuperKRWKMod.Debug;
using System;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace SuperKRWKMod.ModList
{
	/// <summary>
	/// Picks mod UI text for the game's selected language (Settings &gt; General &gt; Language).
	/// English is the fallback for languages without a translation.
	/// </summary>
	public static class ModText
	{
		private static bool _hooked;
		private static Action _languageChanged;

		/// <summary>Raised after the player switches the game's language.</summary>
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

		public static bool IsJapanese
		{
			get
			{
				var code = LocalizationSettings.SelectedLocale?.Identifier.Code;
				return code != null && code.StartsWith("ja");
			}
		}

		public static string Get(string english, string japanese)
		{
			return IsJapanese ? japanese : english;
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
			_languageChanged?.Invoke();
		}
	}
}
