using BongoCat;
using SuperKRWKMod.Utilities;
using System;
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

		/// <summary>Max gap between taps (seconds) that keeps a combo going.</summary>
		public float ComboTimeout { get; set; } = 1f;

		/// <summary>Keys per second over the recent window.</summary>
		public float Kps { get; private set; }

		/// <summary>Kps eased toward its target so effects don't flicker between taps.</summary>
		public float SmoothKps { get; private set; }

		public float BestKps { get; private set; }

		/// <summary>True once the current burst has filled a whole window, so Kps is a fair reading.</summary>
		public bool KpsIsSteady { get; private set; }

		/// <summary>Taps in the current unbroken run; 0 once it has lapsed.</summary>
		public int Combo { get; private set; }

		/// <summary>Length of the last combo that lapsed (for showing its final count).</summary>
		public int LastCombo { get; private set; }

		/// <summary>Raised for every tap with the amount and the seconds since the previous tap.</summary>
		public event Action<int, float> Tapped;

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
			float gap = now - LastTapTime;
			Combo = Combo > 0 && gap <= ComboTimeout ? Combo + amount : amount;
			if (_samples.Count == 0)
				_burstStart = now;
			_samples.Enqueue(new TapSample { Time = now, Amount = amount });
			_samplesTotal += amount;
			SessionTaps += amount;
			LastTapTime = now;
			Tapped?.Invoke(amount, gap);
		}

		private void Update()
		{
			var now = Time.unscaledTime;

			if (Combo > 0 && now - LastTapTime > ComboTimeout)
			{
				LastCombo = Combo;
				Combo = 0;
			}

			while (_samples.Count > 0 && now - _samples.Peek().Time > WindowSeconds)
				_samplesTotal -= _samples.Dequeue().Amount;

			// Until a full window has elapsed since the first tap of a burst, divide by the
			// elapsed time instead so the meter responds immediately.
			float span = WindowSeconds;
			if (_samples.Count > 0)
				span = Mathf.Clamp(now - _samples.Peek().Time, 1f, WindowSeconds);

			Kps = _samplesTotal / span;
			// Only a full window counts toward the best, so a short opening flurry can't inflate it.
			KpsIsSteady = _samples.Count > 0 && now - _burstStart >= WindowSeconds;
			if (Kps > BestKps && KpsIsSteady)
				BestKps = Kps;

			// Rise fast, fall slower.
			float rate = Kps > SmoothKps ? 6f : 1.5f;
			SmoothKps = Mathf.Lerp(SmoothKps, Kps, 1f - MathUtil.Exp(-rate * Time.unscaledDeltaTime));
		}
	}
}
