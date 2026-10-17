using BongoCat;
using SuperKRWKMod.Config;
using SuperKRWKMod.Debug;
using SuperKRWKMod.Utilities;
using System;
using TMPro;
using UnityEngine;

namespace SuperKRWKMod.Features
{
	/// <summary>
	/// After a while without input the cat breathes slowly and floats a "Zzz". Any tap wakes it.
	/// </summary>
	public class SleepMode : MonoBehaviour, IMovableText
	{
		private static readonly Vector2 ZzzBasePosition = new Vector2(44f, 78f);
		private const float BreathPeriod = 4f;
		private const float BreathDepth = 1.5f;

		private ModConfig _config;
		private TapTracker _tracker;
		private CatMotion _motion;
		private TextMeshProUGUI _zzz;
		private readonly RenderDemand _render = new RenderDemand();
		private bool _asleep;
		private float _sleepTime;
		private bool _editing;

		public bool IsAsleep => _asleep;

		public void Init(ModConfig config, TapTracker tracker, CatMotion motion, Transform parent, TMP_Text style)
		{
			_config = config;
			_tracker = tracker;
			_motion = motion;
			_zzz = UiUtil.CreateLabel("SuperKRWKZzz", parent, style, 12f, ZzzBasePosition, new Vector2(60f, 30f));
			_zzz.alignment = TextAlignmentOptions.BottomLeft;
			_zzz.gameObject.SetActive(false);
		}

		private void Update()
		{
			if (_tracker == null || _editing)
				return;

			bool shouldSleep = _config.SleepEnabled && Time.unscaledTime - _tracker.LastTapTime >= _config.EffectiveIdleSeconds(DateTime.Now);

			if (shouldSleep != _asleep)
			{
				if (shouldSleep)
					FallAsleep();
				else
					WakeUp();
			}

			if (_asleep)
				Animate();
		}

		private Vector2 ZzzOrigin => ZzzBasePosition + new Vector2(_config.ZzzOffsetX, _config.ZzzOffsetY);

		// --- IMovableText (layout editor) ---------------------------------------------------------

		public RectTransform LayoutTarget => _zzz.rectTransform;
		public Vector2 LayoutBase => ZzzBasePosition;
		public string LayoutSection => "Sleep";
		public string LayoutKeyX => "ZzzOffsetX";
		public string LayoutKeyY => "ZzzOffsetY";

		public void SetEditing(bool editing)
		{
			_editing = editing;
			if (editing)
			{
				_zzz.text = "z<size=130%>Z</size><size=160%>z</size>";
				var color = _zzz.color;
				color.a = 1f;
				_zzz.color = color;
				_zzz.rectTransform.anchoredPosition = ZzzOrigin;
				_zzz.gameObject.SetActive(true);
			}
			else if (!_asleep)
			{
				_zzz.gameObject.SetActive(false);
			}
		}

		private void FallAsleep()
		{
			_asleep = true;
			_sleepTime = Time.unscaledTime;
			_zzz.gameObject.SetActive(true);
			Log.InfoT(this, "Cat fell asleep.");
		}

		private void WakeUp()
		{
			_asleep = false;
			_zzz.gameObject.SetActive(false);
			_motion.SleepOffset = Vector2.zero;
			_render.SetLowRate(0);
			OnDemandRenderHelper.Instance?.ResumeRenderingForDuration(0.1f);
			Log.InfoT(this, $"Cat woke up after {Time.unscaledTime - _sleepTime:0}s.");
		}

		private void Animate()
		{
			_render.SetLowRate(_config.SleepAnimationFps);

			float t = Time.unscaledTime - _sleepTime;

			// Slow sink-and-rise, like breathing.
			float breath = (1f - Mathf.Cos(t * MathUtil.PI * 2f / BreathPeriod)) * 0.5f;
			_motion.SleepOffset = new Vector2(0f, -breath * BreathDepth);

			// "z", "zZ", "zZz" cycling, drifting up and fading.
			float cycle = (t % 3f) / 3f;
			int letters = 1 + (int)(cycle * 3f);
			_zzz.text = letters == 1 ? "z" : letters == 2 ? "z<size=130%>Z</size>" : "z<size=130%>Z</size><size=160%>z</size>";
			_zzz.rectTransform.anchoredPosition = ZzzOrigin + new Vector2(cycle * 4f, cycle * 8f);
			var color = _zzz.color;
			color.a = Mathf.Clamp01(Mathf.Sin(cycle * MathUtil.PI) * 1.4f);
			_zzz.color = color;
		}

		private void OnDisable()
		{
			if (_asleep)
				WakeUp();
			_render.ReleaseAll();
		}

		private void OnDestroy()
		{
			if (_zzz != null)
				Destroy(_zzz.gameObject);
		}
	}
}
