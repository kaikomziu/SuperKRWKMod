using SuperKRWKMod.Debug;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace SuperKRWKMod.Config
{
	/// <summary>
	/// Minimal INI config stored at &lt;game&gt;\SuperKRWKMod\superkrwk.ini. Created with defaults if missing.
	/// </summary>
	public class ModConfig
	{
		private const string FileName = "superkrwk.ini";

		private const string DefaultContents =
@"; SuperKRWKMod settings. Press F9 in game (with Bongo Cat focused) to reload.
; Visual only: nothing here changes taps, stats, items or drops.

[Zoomies]
; Cat hops when typing faster than BounceKps (keys per second) and glows rainbow above RainbowKps.
Enabled=true
BounceKps=4
RainbowKps=7.5
; Hop height in UI pixels at full intensity.
BounceHeight=10
; Max tilt wobble in degrees at full intensity.
WobbleDegrees=6
; 0..1, how strongly the rainbow tints the cat.
RainbowStrength=0.65

[Meter]
; Typing speed display above the cat. Toggle with ToggleKey.
Enabled=true
Visible=true
ToggleKey=F7
; Offset from the default position, in UI pixels.
OffsetX=0
OffsetY=0
FontSize=11
; Seconds of history used to compute keys per second.
WindowSeconds=3

[Sleep]
; Cat breathes slowly and shows Zzz after IdleSeconds without input.
Enabled=true
IdleSeconds=60
; Close the cat's eyes while asleep.
CloseEyes=true
; Frame rate cap while asleep and the game is in the background (keeps GPU use low).
AnimationFps=12
; Late at night the cat dozes off sooner: NightIdleSeconds between NightStartHour and NightEndHour (0-24).
NightMode=true
NightStartHour=23
NightEndHour=5
NightIdleSeconds=20

[Eyes]
; The cat's eyes follow the mouse cursor.
FollowCursor=true
; How far the pupils move, in sprite pixels (1-6).
MaxShift=3

[Combo]
; Shows x128 COMBO while you keep typing without pausing longer than TimeoutSeconds.
Enabled=true
MinCombo=10
TimeoutSeconds=1
; Offset from the default position, in UI pixels.
OffsetX=0
OffsetY=0

[Chest]
; Tell you when a chest can be bought (you still open it yourself).
Enabled=true
; Speech bubble above the cat.
Bubble=true
; Shake the shop button.
Shake=true
; Remind again every N minutes while it stays unopened (0 = only once).
ReminderMinutes=10
; Time to the next chest, shown left of the combo counter.
Countdown=true
CountdownOffsetX=0
CountdownOffsetY=0

[Text]
; Outline around the mod's on-screen text (meter, combo, countdown, Zzz) for readability.
Outline=true
; Colour as #RRGGBB (or a name like white, black).
OutlineColor=#FFFFFF
; Thickness 0..1.
OutlineWidth=0.35
; Size multiplier for all of the mod's on-screen text.
Scale=1.25
; Box behind the speed meter and chest countdown.
Background=true

[AppRules]
; While one of these apps is running, the listed features are switched off; they come back when it closes.
; Format: <exe name>=<features, comma separated>
; Features: zoomies, rainbow, meter, combo, sleep, eyes, chest, all
A Dance of Fire and Ice.exe=rainbow

[General]
ReloadKey=F9
; Language of the mod's text: auto (follow the game) or a code such as en, ja, zh-CN, ko, es, fr, de...
Language=auto
";

		private readonly Dictionary<string, string> _values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		// Feature names for [AppRules].
		public const string FeatureAll = "all";
		public const string FeatureZoomies = "zoomies";
		public const string FeatureRainbow = "rainbow";
		public const string FeatureMeter = "meter";
		public const string FeatureCombo = "combo";
		public const string FeatureSleep = "sleep";
		public const string FeatureEyes = "eyes";
		public const string FeatureChest = "chest";

		private HashSet<string> _suppressed = new HashSet<string>();

		/// <summary>Features temporarily switched off by app rules (not saved to the ini).</summary>
		public HashSet<string> Suppressed => _suppressed;

		public void SetSuppressed(HashSet<string> features)
		{
			_suppressed = features;
		}

		public bool IsSuppressed(string feature)
		{
			return _suppressed.Contains(feature) || _suppressed.Contains(FeatureAll);
		}

		public static string ModDirectory => Path.Combine(Path.GetDirectoryName(Application.dataPath), "SuperKRWKMod");

		public static string FilePath => Path.Combine(ModDirectory, FileName);

		public bool ZoomiesEnabled => GetBool("Zoomies", "Enabled", true) && !IsSuppressed(FeatureZoomies);
		public bool RainbowEnabled => !IsSuppressed(FeatureRainbow);
		// Older configs used keys per minute (BounceKpm/RainbowKpm); still honoured.
		public float BounceKps => GetFloat("Zoomies", "BounceKps", GetFloat("Zoomies", "BounceKpm", 240f) / 60f);
		public float RainbowKps => GetFloat("Zoomies", "RainbowKps", GetFloat("Zoomies", "RainbowKpm", 450f) / 60f);
		public float BounceHeight => GetFloat("Zoomies", "BounceHeight", 10f);
		public float WobbleDegrees => GetFloat("Zoomies", "WobbleDegrees", 6f);
		public float RainbowStrength => Mathf.Clamp01(GetFloat("Zoomies", "RainbowStrength", 0.65f));

		public bool MeterEnabled => GetBool("Meter", "Enabled", true) && !IsSuppressed(FeatureMeter);
		public bool MeterVisible => GetBool("Meter", "Visible", true);
		public KeyCode MeterToggleKey => GetKey("Meter", "ToggleKey", KeyCode.F7);
		public float MeterOffsetX => GetFloat("Meter", "OffsetX", 0f);
		public float MeterOffsetY => GetFloat("Meter", "OffsetY", 0f);
		public float MeterFontSize => Mathf.Max(4f, GetFloat("Meter", "FontSize", 11f));
		public float WindowSeconds => Mathf.Clamp(GetFloat("Meter", "WindowSeconds", 3f), 0.5f, 60f);

		public bool SleepEnabled => GetBool("Sleep", "Enabled", true) && !IsSuppressed(FeatureSleep);
		public float IdleSeconds => Mathf.Max(5f, GetFloat("Sleep", "IdleSeconds", 60f));
		public bool SleepCloseEyes => GetBool("Sleep", "CloseEyes", true);
		public int SleepAnimationFps => Mathf.Clamp((int)GetFloat("Sleep", "AnimationFps", 12f), 1, 60);
		public float ZzzOffsetX => GetFloat("Sleep", "ZzzOffsetX", 0f);
		public float ZzzOffsetY => GetFloat("Sleep", "ZzzOffsetY", 0f);
		public bool NightMode => GetBool("Sleep", "NightMode", true);
		public float NightStartHour => GetFloat("Sleep", "NightStartHour", 23f);
		public float NightEndHour => GetFloat("Sleep", "NightEndHour", 5f);
		public float NightIdleSeconds => Mathf.Max(5f, GetFloat("Sleep", "NightIdleSeconds", 20f));

		public bool EyesFollowCursor => GetBool("Eyes", "FollowCursor", true) && !IsSuppressed(FeatureEyes);
		public int EyesMaxShift => (int)GetFloat("Eyes", "MaxShift", 3f);

		public bool ComboEnabled => GetBool("Combo", "Enabled", true) && !IsSuppressed(FeatureCombo);
		public int ComboMin => Mathf.Max(2, (int)GetFloat("Combo", "MinCombo", 10f));
		public float ComboTimeout => Mathf.Clamp(GetFloat("Combo", "TimeoutSeconds", 1f), 0.2f, 10f);
		public float ComboOffsetX => GetFloat("Combo", "OffsetX", 0f);
		public float ComboOffsetY => GetFloat("Combo", "OffsetY", 0f);

		public bool ChestEnabled => GetBool("Chest", "Enabled", true) && !IsSuppressed(FeatureChest);
		public bool ChestBubble => GetBool("Chest", "Bubble", true);
		public bool ChestShake => GetBool("Chest", "Shake", true);
		public bool TextOutline => GetBool("Text", "Outline", true);
		public string TextOutlineColorName => Get("Text", "OutlineColor") ?? "#FFFFFF";
		public float TextOutlineWidth => Mathf.Clamp01(GetFloat("Text", "OutlineWidth", 0.35f));
		public float TextScale => Mathf.Clamp(GetFloat("Text", "Scale", 1.25f), 0.5f, 3f);
		public bool TextBackground => GetBool("Text", "Background", true);

		public Color TextOutlineColor
		{
			get
			{
				var name = TextOutlineColorName.Trim();
				if (!name.StartsWith("#") && name.Length == 6)
					name = "#" + name;
				return ColorUtility.TryParseHtmlString(name, out var color) ? color : Color.white;
			}
		}

		public bool ChestCountdown => GetBool("Chest", "Countdown", true);
		public float CountdownOffsetX => GetFloat("Chest", "CountdownOffsetX", 0f);
		public float CountdownOffsetY => GetFloat("Chest", "CountdownOffsetY", 0f);
		public float ChestReminderMinutes => Mathf.Max(0f, GetFloat("Chest", "ReminderMinutes", 10f));

		/// <summary>Whether the local clock is inside the night window (which may wrap past midnight).</summary>
		public bool IsNight(DateTime now)
		{
			float hour = now.Hour + now.Minute / 60f;
			float start = NightStartHour, end = NightEndHour;
			return start <= end ? hour >= start && hour < end : hour >= start || hour < end;
		}

		/// <summary>Idle time before sleeping, shorter at night when NightMode is on.</summary>
		public float EffectiveIdleSeconds(DateTime now)
		{
			return NightMode && IsNight(now) ? Mathf.Min(IdleSeconds, NightIdleSeconds) : IdleSeconds;
		}

		public string Language => Get("General", "Language") ?? "auto";
		public KeyCode ReloadKey => GetKey("General", "ReloadKey", KeyCode.F9);

		public void Load()
		{
			_values.Clear();

			try
			{
				if (!File.Exists(FilePath))
				{
					Directory.CreateDirectory(ModDirectory);
					File.WriteAllText(FilePath, DefaultContents);
					Log.InfoT(this, $"Created default config at {FilePath}");
				}

				Parse(File.ReadAllText(FilePath));
				Log.InfoT(this, $"Loaded config ({_values.Count} values).");
			}
			catch (Exception e)
			{
				Log.ExceptionT(this, e, "Failed to load config, using defaults");
				_values.Clear();
				Parse(DefaultContents);
			}
		}

		/// <summary>
		/// Updates one value in place in the file, keeping comments and layout.
		/// </summary>
		/// <summary>Deletes one key from the file, keeping everything else as is.</summary>
		public void Remove(string section, string key)
		{
			_values.Remove(section + "." + key);

			try
			{
				var lines = new List<string>(File.ReadAllText(FilePath).Replace("\r\n", "\n").Split('\n'));
				string current = null;
				for (int i = 0; i < lines.Count; i++)
				{
					var line = lines[i].Trim();
					if (line.StartsWith("[") && line.EndsWith("]"))
					{
						current = line.Substring(1, line.Length - 2).Trim();
						continue;
					}

					int eq = line.IndexOf('=');
					if (string.Equals(current, section, StringComparison.OrdinalIgnoreCase) && eq > 0
						&& string.Equals(line.Substring(0, eq).Trim(), key, StringComparison.OrdinalIgnoreCase))
					{
						lines.RemoveAt(i);
						File.WriteAllText(FilePath, string.Join("\r\n", lines.ToArray()));
						return;
					}
				}
			}
			catch (Exception e)
			{
				Log.ExceptionT(this, e, $"Failed to remove {section}.{key}");
			}
		}

		public void Set(string section, string key, string value)
		{
			_values[section + "." + key] = value;

			try
			{
				var lines = new List<string>(File.ReadAllText(FilePath).Replace("\r\n", "\n").Split('\n'));
				string current = null;
				int sectionEnd = -1;

				for (int i = 0; i < lines.Count; i++)
				{
					var line = lines[i].Trim();
					if (line.StartsWith("[") && line.EndsWith("]"))
					{
						current = line.Substring(1, line.Length - 2).Trim();
						continue;
					}

					if (!string.Equals(current, section, StringComparison.OrdinalIgnoreCase))
						continue;

					sectionEnd = i + 1;
					int eq = line.IndexOf('=');
					if (eq > 0 && string.Equals(line.Substring(0, eq).Trim(), key, StringComparison.OrdinalIgnoreCase))
					{
						lines[i] = key + "=" + value;
						File.WriteAllText(FilePath, string.Join("\r\n", lines.ToArray()));
						return;
					}
				}

				if (sectionEnd < 0)
				{
					lines.Add("[" + section + "]");
					sectionEnd = lines.Count;
				}

				lines.Insert(sectionEnd, key + "=" + value);
				File.WriteAllText(FilePath, string.Join("\r\n", lines.ToArray()));
			}
			catch (Exception e)
			{
				Log.ExceptionT(this, e, $"Failed to save {section}.{key}");
			}
		}

		private void Parse(string text)
		{
			string section = "";

			foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
			{
				var line = raw.Trim();
				if (line.Length == 0 || line.StartsWith(";") || line.StartsWith("#"))
					continue;

				if (line.StartsWith("[") && line.EndsWith("]"))
				{
					section = line.Substring(1, line.Length - 2).Trim();
					continue;
				}

				int eq = line.IndexOf('=');
				if (eq <= 0)
					continue;

				_values[section + "." + line.Substring(0, eq).Trim()] = line.Substring(eq + 1).Trim();
			}
		}

		/// <summary>All key/value pairs of a section, in no particular order.</summary>
		public List<KeyValuePair<string, string>> GetSection(string section)
		{
			var result = new List<KeyValuePair<string, string>>();
			var prefix = section + ".";
			foreach (var pair in _values)
			{
				if (pair.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
					result.Add(new KeyValuePair<string, string>(pair.Key.Substring(prefix.Length), pair.Value));
			}
			return result;
		}

		private string Get(string section, string key)
		{
			return _values.TryGetValue(section + "." + key, out var value) ? value : null;
		}

		public bool GetBool(string section, string key, bool fallback)
		{
			var value = Get(section, key);
			if (value == null)
				return fallback;

			switch (value.ToLowerInvariant())
			{
				case "true": case "1": case "yes": case "on": return true;
				case "false": case "0": case "no": case "off": return false;
				default: return fallback;
			}
		}

		private float GetFloat(string section, string key, float fallback)
		{
			var value = Get(section, key);
			return value != null && float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
				? result
				: fallback;
		}

		private KeyCode GetKey(string section, string key, KeyCode fallback)
		{
			var value = Get(section, key);
			if (string.IsNullOrEmpty(value))
				return fallback;

			// Enum.Parse is avoided on purpose; scan the known values instead.
			foreach (KeyCode code in Enum.GetValues(typeof(KeyCode)))
			{
				if (string.Equals(code.ToString(), value, StringComparison.OrdinalIgnoreCase))
					return code;
			}

			return fallback;
		}
	}
}
