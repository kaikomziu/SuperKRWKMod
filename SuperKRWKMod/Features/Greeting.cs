using SuperKRWKMod.Config;
using SuperKRWKMod.ModList;
using System;
using System.Collections;
using System.Globalization;
using UnityEngine;

namespace SuperKRWKMod.Features
{
	/// <summary>
	/// The cat says hello for the time of day when the game starts, and once a night suggests bed
	/// when it gets late while you're still typing.
	/// </summary>
	public class Greeting : MonoBehaviour
	{
		private const float StartDelay = 3f;

		private ModConfig _config;
		private TapTracker _tracker;
		private CatBubble _bubble;
		private string _lateNightShownFor;

		public void Init(ModConfig config, TapTracker tracker, CatBubble bubble)
		{
			_config = config;
			_tracker = tracker;
			_bubble = bubble;
			StartCoroutine(Run());
		}

		private IEnumerator Run()
		{
			yield return new WaitForSeconds(StartDelay);

			var now = DateTime.Now;
			if (_config.GreetingEnabled)
				_bubble.Say(Hello(now.Hour), 6f);

			// Starting during the night counts as tonight's reminder already.
			if (_config.IsNight(now))
				_lateNightShownFor = NightKey(now);

			var wait = new WaitForSeconds(30f);
			while (true)
			{
				yield return wait;
				now = DateTime.Now;
				bool active = Time.unscaledTime - _tracker.LastTapTime < 120f;
				if (!_config.GreetingEnabled || !_config.IsNight(now) || !active || _lateNightShownFor == NightKey(now))
					continue;

				_lateNightShownFor = NightKey(now);
				_bubble.Say(ModText.Format("It's {0} o'clock. Time for bed?", "もう{0}時だよ。そろそろ寝る？", now.Hour), 8f);
			}
		}

		private static string Hello(int hour)
		{
			if (hour >= 5 && hour < 11)
				return ModText.Get("Good morning! Let's have a good day", "おはよう！今日もがんばろう");
			if (hour >= 11 && hour < 17)
				return ModText.Get("Hello!", "こんにちは！");
			if (hour >= 17 && hour < 23)
				return ModText.Get("Good evening! Nice work today", "こんばんは！今日もおつかれさま");
			return ModText.Get("Up late… don't overdo it", "夜ふかしだね…ほどほどにね");
		}

		/// <summary>One key per night: hours after midnight belong to the previous evening.</summary>
		private static string NightKey(DateTime now)
		{
			var evening = now.Hour < 12 ? now.AddDays(-1) : now;
			return evening.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
		}
	}
}
