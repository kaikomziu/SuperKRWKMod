using SuperKRWKMod.Config;
using SuperKRWKMod.Debug;
using SuperKRWKMod.ModList;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace SuperKRWKMod.Features
{
	/// <summary>
	/// The mod's own typing records per day (taps, active time, best KPS, longest combo), kept in
	/// SuperKRWKMod\records.csv. Counted from the taps the mod observes; the game's stats are untouched.
	/// </summary>
	public class Records : MonoBehaviour
	{
		private const string FileName = "records.csv";
		private const string Header = "date,taps,activeSeconds,bestKps,bestCombo";
		private const float SaveInterval = 60f;
		// Gaps between taps up to this long count as time spent typing.
		private const float ActiveGap = 2f;

		private class Day
		{
			public string Date;
			public long Taps;
			public float ActiveSeconds;
			public float BestKps;
			public int BestCombo;
		}

		// (SortedDictionary is stripped from the game; rows are sorted when saving instead.)
		private readonly Dictionary<string, Day> _days = new Dictionary<string, Day>(StringComparer.Ordinal);
		private TapTracker _tracker;
		private bool _dirty;
		private float _nextSave;

		private static string FilePath => Path.Combine(ModConfig.ModDirectory, FileName);
		private static string TodayKey => DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

		public void Init(TapTracker tracker)
		{
			_tracker = tracker;
			_tracker.Tapped += OnTapped;
			Load();
			_nextSave = Time.unscaledTime + SaveInterval;
		}

		private Day Today
		{
			get
			{
				var key = TodayKey;
				if (!_days.TryGetValue(key, out var day))
				{
					day = new Day { Date = key };
					_days[key] = day;
				}
				return day;
			}
		}

		private void OnTapped(int amount, float gap)
		{
			var today = Today;
			today.Taps += amount;
			if (gap <= ActiveGap)
				today.ActiveSeconds += gap;
			_dirty = true;
		}

		private void Update()
		{
			if (_tracker == null)
				return;

			if (_tracker.Combo > 0 || _tracker.KpsIsSteady)
			{
				var today = Today;
				if (_tracker.Combo > today.BestCombo)
				{
					today.BestCombo = _tracker.Combo;
					_dirty = true;
				}
				if (_tracker.KpsIsSteady && _tracker.Kps > today.BestKps)
				{
					today.BestKps = _tracker.Kps;
					_dirty = true;
				}
			}

			if (_dirty && Time.unscaledTime >= _nextSave)
				Save();
		}

		private void OnApplicationQuit()
		{
			Save();
		}

		private void OnDestroy()
		{
			if (_tracker != null)
				_tracker.Tapped -= OnTapped;
			Save();
		}

		/// <summary>Text for the Mods settings page.</summary>
		public string Describe()
		{
			var today = Today;
			long totalTaps = 0;
			float allBestKps = 0f;
			int allBestCombo = 0;
			Day bestDay = null;
			foreach (var day in _days.Values)
			{
				totalTaps += day.Taps;
				allBestKps = Mathf.Max(allBestKps, day.BestKps);
				allBestCombo = Mathf.Max(allBestCombo, day.BestCombo);
				if (bestDay == null || day.Taps > bestDay.Taps)
					bestDay = day;
			}

			var sb = new StringBuilder();
			sb.Append("<b>").Append(ModText.Get("Records", "記録")).Append("</b>\n");

			sb.Append(ModText.Get("Today: ", "今日: "))
				.Append(Number(today.Taps)).Append(ModText.Get(" taps", " タップ"))
				.Append(ModText.Get(" / typing ", " / 打っていた時間 ")).Append(Duration(today.ActiveSeconds)).Append('\n');
			sb.Append(ModText.Get("Today's best: ", "今日の最高: "))
				.Append(Kps(today.BestKps)).Append(" KPS / ")
				.Append(ModText.Get("combo ", "コンボ ")).Append(Number(today.BestCombo)).Append('\n');
			sb.Append(ModText.Get("All time: ", "これまで: "))
				.Append(Number(totalTaps)).Append(ModText.Get(" taps", " タップ"))
				.Append(" / ").Append(Kps(allBestKps)).Append(" KPS / ")
				.Append(ModText.Get("combo ", "コンボ ")).Append(Number(allBestCombo));
			if (bestDay != null && bestDay.Taps > 0)
			{
				sb.Append('\n').Append(ModText.Get("Best day: ", "一番打った日: "))
					.Append(ShortDate(bestDay.Date)).Append(' ').Append(Number(bestDay.Taps)).Append(ModText.Get(" taps", " タップ"));
			}

			// Last 7 days, newest first.
			sb.Append("\n\n<b>").Append(ModText.Get("Last 7 days", "最近7日")).Append("</b>");
			for (int i = 0; i < 7; i++)
			{
				var key = DateTime.Now.AddDays(-i).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
				_days.TryGetValue(key, out var day);
				sb.Append('\n').Append(ShortDate(key)).Append("  ")
					.Append(day != null ? Number(day.Taps) : "-")
					.Append(day != null && day.Taps > 0 ? "  (" + Duration(day.ActiveSeconds) + ")" : "");
			}

			return sb.ToString();
		}

		private static string Number(long value)
		{
			return value.ToString("#,0", CultureInfo.InvariantCulture);
		}

		private static string Kps(float value)
		{
			return value.ToString("0.0", CultureInfo.InvariantCulture);
		}

		private static string ShortDate(string date)
		{
			// yyyy-MM-dd -> M/d
			var parts = date.Split('-');
			return parts.Length == 3 ? $"{int.Parse(parts[1], CultureInfo.InvariantCulture)}/{int.Parse(parts[2], CultureInfo.InvariantCulture)}" : date;
		}

		private static string Duration(float seconds)
		{
			int minutes = (int)(seconds / 60f);
			int hours = minutes / 60;
			minutes %= 60;
			if (hours > 0)
				return ModText.Get($"{hours}h {minutes}m", $"{hours}時間{minutes}分");
			if (minutes == 0 && seconds > 0f)
				return ModText.Get("<1m", "1分未満");
			return ModText.Get($"{minutes}m", $"{minutes}分");
		}

		private void Load()
		{
			_days.Clear();
			try
			{
				if (!File.Exists(FilePath))
					return;

				foreach (var raw in File.ReadAllText(FilePath).Replace("\r\n", "\n").Split('\n'))
				{
					var line = raw.Trim();
					if (line.Length == 0 || line == Header)
						continue;

					var cols = line.Split(',');
					if (cols.Length < 5)
						continue;

					var day = new Day { Date = cols[0] };
					long.TryParse(cols[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out day.Taps);
					float.TryParse(cols[2], NumberStyles.Float, CultureInfo.InvariantCulture, out day.ActiveSeconds);
					float.TryParse(cols[3], NumberStyles.Float, CultureInfo.InvariantCulture, out day.BestKps);
					int.TryParse(cols[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out day.BestCombo);
					_days[day.Date] = day;
				}

				Log.InfoT(this, $"Loaded {_days.Count} day(s) of records.");
			}
			catch (Exception e)
			{
				Log.ExceptionT(this, e, "Failed to load records");
			}
		}

		private void Save()
		{
			_nextSave = Time.unscaledTime + SaveInterval;
			if (!_dirty)
				return;

			try
			{
				var sb = new StringBuilder(Header).Append("\r\n");
				var keys = new List<string>(_days.Keys);
				keys.Sort(string.CompareOrdinal);
				foreach (var key in keys)
				{
					var day = _days[key];
					sb.Append(day.Date).Append(',')
						.Append(day.Taps.ToString(CultureInfo.InvariantCulture)).Append(',')
						.Append(day.ActiveSeconds.ToString("0", CultureInfo.InvariantCulture)).Append(',')
						.Append(day.BestKps.ToString("0.0", CultureInfo.InvariantCulture)).Append(',')
						.Append(day.BestCombo.ToString(CultureInfo.InvariantCulture)).Append("\r\n");
				}

				Directory.CreateDirectory(ModConfig.ModDirectory);
				// Write a temp file then swap, so a crash mid-write can't wipe the records.
				var temp = FilePath + ".tmp";
				File.WriteAllText(temp, sb.ToString());
				File.Copy(temp, FilePath, true); // (File.Move is stripped from the game)
				File.Delete(temp);
				_dirty = false;
			}
			catch (Exception e)
			{
				Log.ExceptionT(this, e, "Failed to save records");
			}
		}
	}
}
