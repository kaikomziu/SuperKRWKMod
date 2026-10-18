using SuperKRWKMod.Config;
using SuperKRWKMod.ModList;
using UnityEngine;

namespace SuperKRWKMod.Features
{
	/// <summary>
	/// After typing for a long stretch without a real pause, the cat suggests a break, and again every
	/// so often after that. Only a reminder: taps keep counting as usual.
	/// </summary>
	public class BreakReminder : MonoBehaviour
	{
		private const float CheckSeconds = 1f;

		private ModConfig _config;
		private TapTracker _tracker;
		private CatBubble _bubble;
		private float _stretchStart = -1f;
		private float _lastTap = -9999f;
		private int _reminders;
		private float _nextCheck;

		public void Init(ModConfig config, TapTracker tracker, CatBubble bubble)
		{
			_config = config;
			_tracker = tracker;
			_bubble = bubble;
			_tracker.Tapped += OnTapped;
		}

		private void OnDestroy()
		{
			if (_tracker != null)
				_tracker.Tapped -= OnTapped;
		}

		private void OnTapped(int amount, float gap)
		{
			float now = Time.unscaledTime;
			// A pause this long counts as a break: start a new stretch.
			if (_stretchStart < 0f || now - _lastTap >= _config.BreakResetMinutes * 60f)
			{
				_stretchStart = now;
				_reminders = 0;
			}
			_lastTap = now;
		}

		private void Update()
		{
			if (Time.unscaledTime < _nextCheck)
				return;
			_nextCheck = Time.unscaledTime + CheckSeconds;

			if (!_config.BreakEnabled || _stretchStart < 0f)
				return;

			float now = Time.unscaledTime;
			// Only while still at it (not already resting).
			if (now - _lastTap >= _config.BreakResetMinutes * 60f)
				return;

			float interval = _config.BreakMinutes * 60f;
			float typing = now - _stretchStart;
			if (typing < interval * (_reminders + 1))
				return;

			_reminders++;
			int minutes = Mathf.RoundToInt(typing / 60f);
			_bubble.Say(ModText.Format(
				"{0} minutes without a break. Time for a little rest?",
				"{0}分打ちっぱなしだよ。ちょっと休憩しよ？", minutes), 8f);
		}
	}
}
