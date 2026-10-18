using BongoCat;
using SuperKRWKMod.Config;
using SuperKRWKMod.Debug;
using SuperKRWKMod.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SuperKRWKMod.Features
{
	/// <summary>
	/// Drives the cat's eyes by swapping the body image for generated variants: closed while asleep,
	/// otherwise looking toward the mouse cursor. The game keeps setting its own sprites (taps, skins);
	/// this re-applies the right variant of whatever it set, every frame before rendering.
	/// </summary>
	public class CatFace : MonoBehaviour
	{
		private const int Closed = -1;
		private const int Happy = -2;
		private const float BlinkSeconds = 0.12f;
		private const float HappySeconds = 1.6f;
		private const int ComboMilestone = 100;
		private const int Center = 8;
		// Sector hysteresis so the eyes don't flicker on a direction boundary (degrees).
		private const float Hysteresis = 8f;

		private class Variants
		{
			public CatEyes Eyes; // null when the sprite has no detectable eyes
			public readonly Dictionary<int, Sprite> Sprites = new Dictionary<int, Sprite>();
		}

		private ModConfig _config;
		private SleepMode _sleep;
		private Image _body;
		private RectTransform _bodyRect;
		private readonly Dictionary<Sprite, Variants> _bySource = new Dictionary<Sprite, Variants>();
		private readonly Dictionary<Sprite, Sprite> _sourceOf = new Dictionary<Sprite, Sprite>();
		private int _sector = Center;
		private TapTracker _tracker;
		private Transform _visuals;
		private float _nextBlink;
		private float _blinkUntil = -1f;
		private bool _doubleBlink;
		private float _happyUntil = -1f;
		private int _lastComboMilestone;

		public void Init(ModConfig config, SleepMode sleep, Image body, TapTracker tracker, Transform visuals)
		{
			_config = config;
			_sleep = sleep;
			_body = body;
			_bodyRect = body.rectTransform;
			_tracker = tracker;
			_visuals = visuals;
			ScheduleBlink();
		}

		/// <summary>Shows happy "^ ^" eyes for a moment.</summary>
		public void ShowHappy(float seconds = HappySeconds)
		{
			if (_config.EyesHappy)
				_happyUntil = Mathf.Max(_happyUntil, Time.unscaledTime + seconds);
		}

		private void ScheduleBlink()
		{
			_nextBlink = Time.unscaledTime + UnityEngine.Random.Range(2.5f, 6f);
		}

		/// <summary>Patting (yours or a friend's) squishes the cat's visuals unevenly; that's the cue.</summary>
		private bool IsBeingPatted()
		{
			if (_visuals == null)
				return false;
			var scale = _visuals.localScale;
			return Mathf.Abs(scale.y) > 0.0001f && Mathf.Abs(Mathf.Abs(scale.x / scale.y) - 1f) > 0.05f;
		}

		private void UpdateExpressions()
		{
			float now = Time.unscaledTime;

			if (IsBeingPatted())
				ShowHappy();

			int milestone = _tracker != null ? _tracker.Combo / ComboMilestone : 0;
			if (milestone > _lastComboMilestone)
				ShowHappy();
			_lastComboMilestone = milestone;

			if (now >= _nextBlink)
			{
				if (_config.EyesBlink)
					_blinkUntil = now + BlinkSeconds;
				// Sometimes a quick double blink.
				_doubleBlink = !_doubleBlink && UnityEngine.Random.Range(0f, 1f) < 0.25f;
				if (_doubleBlink)
					_nextBlink = now + BlinkSeconds * 2.5f;
				else
					ScheduleBlink();
			}
		}

		private void LateUpdate()
		{
			if (_body == null || _body.sprite == null)
				return;

			var current = _body.sprite;
			var source = _sourceOf.TryGetValue(current, out var s) ? s : current;

			UpdateExpressions();
			float now = Time.unscaledTime;

			// Asleep > happy > blink > looking at the cursor.
			int wanted = Center;
			if (_sleep.IsAsleep && _config.SleepCloseEyes)
				wanted = Closed;
			else if (now < _happyUntil)
				wanted = Happy;
			else if (now < _blinkUntil)
				wanted = Closed;
			else if (_config.EyesFollowCursor)
				wanted = LookSector();

			var target = wanted == Center ? source : GetVariant(source, wanted);
			if (target == null)
				target = source;

			if (target != current)
			{
				_body.sprite = target;
				// The game may have paused rendering in the background; show the change.
				OnDemandRenderHelper.Instance?.ResumeRenderingForDuration(0.05f);
			}
		}

		/// <summary>0..7 = direction (0 = right, counter-clockwise), Center when the cursor is on the cat.</summary>
		private int LookSector()
		{
			// The main canvas is screen-space overlay, so world positions are screen pixels.
			Vector2 center = _bodyRect.TransformPoint(_bodyRect.rect.center);
			Vector2 toCursor = (Vector2)Input.mousePosition - center;
			float catRadius = _bodyRect.rect.width * 0.35f * _bodyRect.lossyScale.y;
			if (toCursor.sqrMagnitude < catRadius * catRadius)
			{
				_sector = Center;
				return Center;
			}

			// Into the sprite's own space, so flipped or rotated cats still look the right way.
			// (InverseTransformVector is stripped; the difference of two points gives the same direction.)
			Vector2 local = _bodyRect.InverseTransformPoint(center + toCursor) - _bodyRect.InverseTransformPoint(center);
			float angle = Mathf.Atan2(local.y, local.x) * 180f / MathUtil.PI;
			if (angle < 0f)
				angle += 360f;

			if (_sector != Center)
			{
				float sectorAngle = _sector * 45f;
				float diff = Mathf.Abs(MathUtil.DeltaAngle(angle, sectorAngle));
				if (diff <= 22.5f + Hysteresis)
					return _sector;
			}

			_sector = (int)((angle + 22.5f) / 45f) % 8;
			return _sector;
		}

		private Sprite GetVariant(Sprite source, int key)
		{
			if (!_bySource.TryGetValue(source, out var variants))
			{
				variants = new Variants();
				try
				{
					variants.Eyes = CatEyes.Analyze(source);
					Log.InfoT(this, variants.Eyes != null
						? $"Found {variants.Eyes.EyeCount} eye(s) on {source.name}."
						: $"No eyes found on {source.name}; leaving it as is.");
				}
				catch (Exception e)
				{
					Log.ExceptionT(this, e, $"Failed to read eyes on {source.name}");
				}

				_bySource[source] = variants;
			}

			if (variants.Eyes == null)
				return null;

			if (!variants.Sprites.TryGetValue(key, out var sprite))
			{
				sprite = key == Closed ? variants.Eyes.MakeClosed()
					: key == Happy ? variants.Eyes.MakeHappy()
					: MakeLooking(variants.Eyes, key);
				variants.Sprites[key] = sprite;
				_sourceOf[sprite] = source;
			}

			return sprite;
		}

		private Sprite MakeLooking(CatEyes eyes, int sector)
		{
			int shift = Mathf.Clamp(_config.EyesMaxShift, 1, 6);
			int diagonal = Mathf.Max(1, Mathf.RoundToInt(shift * 0.7f));
			switch (sector)
			{
				case 0: return eyes.MakeLooking(shift, 0);
				case 1: return eyes.MakeLooking(diagonal, diagonal);
				case 2: return eyes.MakeLooking(0, shift);
				case 3: return eyes.MakeLooking(-diagonal, diagonal);
				case 4: return eyes.MakeLooking(-shift, 0);
				case 5: return eyes.MakeLooking(-diagonal, -diagonal);
				case 6: return eyes.MakeLooking(0, -shift);
				default: return eyes.MakeLooking(diagonal, -diagonal);
			}
		}

		private void OnDisable()
		{
			if (_body != null && _body.sprite != null && _sourceOf.TryGetValue(_body.sprite, out var source))
				_body.sprite = source;
		}
	}
}
