using BongoCat;
using SuperKRWKMod.Utilities;
using System.Collections.Generic;
using UnityEngine;

namespace SuperKRWKMod.Features
{
	/// <summary>
	/// Read-only listener on the main cat's taps. Keeps local session numbers for the visuals;
	/// never touches the game's own counters or Steam stats.
	/// </summary>
	public class TapTracker : MonoBehaviour
	{
		private struct TapSample
		{
			public float Time;
			public int Amount;
		}

		private readonly Queue<TapSample> _samples = new Queue<TapSample>();
		private int _samplesTotal;
		private float _burstStart;
		private Cat _cat;

		public float WindowSeconds { get; set; } = 3f;

		/// <summary>Keys per minute over the recent window.</summary>
		public float Kpm { get; private set; }

		/// <summary>Kpm eased toward its target so effects don't flicker between taps.</summary>
		public float SmoothKpm { get; private set; }

		public float BestKpm { get; private set; }

		public long SessionTaps { get; private set; }

		/// <summary>Unscaled time of the last tap; game start counts as activity.</summary>
		public float LastTapTime { get; private set; }

		public void Attach(Cat cat)
		{
			_cat = cat;
			_cat.OnTap.AddListener(OnTap);
			LastTapTime = Time.unscaledTime;
		}

		private void OnDestroy()
		{
			if (_cat != null)
				_cat.OnTap.RemoveListener(OnTap);
		}

		private void OnTap(int amount)
		{
			var now = Time.unscaledTime;
			if (_samples.Count == 0)
				_burstStart = now;
			_samples.Enqueue(new TapSample { Time = now, Amount = amount });
			_samplesTotal += amount;
			SessionTaps += amount;
			LastTapTime = now;
		}

		private void Update()
		{
			var now = Time.unscaledTime;

			while (_samples.Count > 0 && now - _samples.Peek().Time > WindowSeconds)
				_samplesTotal -= _samples.Dequeue().Amount;

			// Until a full window has elapsed since the first tap of a burst, divide by the
			// elapsed time instead so the meter responds immediately.
			float span = WindowSeconds;
			if (_samples.Count > 0)
				span = Mathf.Clamp(now - _samples.Peek().Time, 1f, WindowSeconds);

			Kpm = _samplesTotal * 60f / span;
			// Only a full window counts toward the best, so a short opening flurry can't inflate it.
			if (Kpm > BestKpm && _samples.Count > 0 && now - _burstStart >= WindowSeconds)
				BestKpm = Kpm;

			// Rise fast, fall slower.
			float rate = Kpm > SmoothKpm ? 6f : 1.5f;
			SmoothKpm = Mathf.Lerp(SmoothKpm, Kpm, 1f - MathUtil.Exp(-rate * Time.unscaledDeltaTime));
		}
	}
}
