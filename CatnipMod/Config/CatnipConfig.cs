using CatnipMod.Debug;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

namespace CatnipMod.Config
{
	/// <summary>
	/// Minimal INI config stored at &lt;game&gt;\CatnipMod\catnip.ini. Created with defaults if missing.
	/// </summary>
	public class CatnipConfig
	{
		private const string FileName = "catnip.ini";

		private const string DefaultContents =
@"; Catnip Mod settings. Press F9 in game (with Bongo Cat focused) to reload.
; Visual only: nothing here changes taps, stats, items or drops.

[Zoomies]
; Cat hops when typing faster than BounceKpm and glows rainbow above RainbowKpm.
Enabled=true
BounceKpm=250
RainbowKpm=450
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
; Seconds of history used to compute keys per minute.
WindowSeconds=3

[Sleep]
; Cat breathes slowly and shows Zzz after IdleSeconds without input.
Enabled=true
IdleSeconds=60
; Frame rate cap while asleep and the game is in the background (keeps GPU use low).
AnimationFps=12

[General]
ReloadKey=F9
";

		private readonly Dictionary<string, string> _values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		public static string ModDirectory => Path.Combine(Path.GetDirectoryName(Application.dataPath), "CatnipMod");

		public static string FilePath => Path.Combine(ModDirectory, FileName);

		public bool ZoomiesEnabled => GetBool("Zoomies", "Enabled", true);
		public float BounceKpm => GetFloat("Zoomies", "BounceKpm", 250f);
		public float RainbowKpm => GetFloat("Zoomies", "RainbowKpm", 450f);
		public float BounceHeight => GetFloat("Zoomies", "BounceHeight", 10f);
		public float WobbleDegrees => GetFloat("Zoomies", "WobbleDegrees", 6f);
		public float RainbowStrength => Mathf.Clamp01(GetFloat("Zoomies", "RainbowStrength", 0.65f));

		public bool MeterEnabled => GetBool("Meter", "Enabled", true);
		public bool MeterVisible => GetBool("Meter", "Visible", true);
		public KeyCode MeterToggleKey => GetKey("Meter", "ToggleKey", KeyCode.F7);
		public float MeterOffsetX => GetFloat("Meter", "OffsetX", 0f);
		public float MeterOffsetY => GetFloat("Meter", "OffsetY", 0f);
		public float MeterFontSize => Mathf.Max(4f, GetFloat("Meter", "FontSize", 11f));
		public float WindowSeconds => Mathf.Clamp(GetFloat("Meter", "WindowSeconds", 3f), 0.5f, 60f);

		public bool SleepEnabled => GetBool("Sleep", "Enabled", true);
		public float IdleSeconds => Mathf.Max(5f, GetFloat("Sleep", "IdleSeconds", 60f));
		public int SleepAnimationFps => Mathf.Clamp((int)GetFloat("Sleep", "AnimationFps", 12f), 1, 60);

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

		private string Get(string section, string key)
		{
			return _values.TryGetValue(section + "." + key, out var value) ? value : null;
		}

		private bool GetBool(string section, string key, bool fallback)
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
