using SuperKRWKMod.Config;
using SuperKRWKMod.Utilities;
using UnityEngine;
using UnityEngine.UI;

namespace SuperKRWKMod.Features
{
	/// <summary>
	/// The faster you type, the more the cat hops and wobbles; past a threshold it glows rainbow.
	/// </summary>
	public class Zoomies : MonoBehaviour
	{
		private ModConfig _config;
		private TapTracker _tracker;
		private CatMotion _motion;
		private Graphic[] _tintTargets;
		private Color[] _originalColors;
		private readonly RenderDemand _render = new RenderDemand();

		private float _intensity;
		private float _rainbow;
		private float _phase;
		private float _hue;
		private bool _tinted;

		public void Init(ModConfig config, TapTracker tracker, CatMotion motion, Graphic[] tintTargets)
		{
			_config = config;
			_tracker = tracker;
			_motion = motion;
			_tintTargets = tintTargets;
		}

		private void Update()
		{
			if (_tracker == null)
				return;

			bool enabled = _config.ZoomiesEnabled;
			float kps = _tracker.SmoothKps;
			float bounceKps = _config.BounceKps;
			float rainbowKps = Mathf.Max(_config.RainbowKps, bounceKps + 0.1f);

			float targetIntensity = enabled ? Mathf.Clamp01((kps - bounceKps) / (rainbowKps - bounceKps) * 0.7f + (kps >= bounceKps ? 0.3f : 0f)) : 0f;
			float targetRainbow = enabled ? Mathf.Clamp01((kps - rainbowKps) / 1f) : 0f;

			float dt = Time.unscaledDeltaTime;
			_intensity = MathUtil.MoveTowards(_intensity, targetIntensity, dt * 2.5f);
			_rainbow = MathUtil.MoveTowards(_rainbow, targetRainbow, dt * 1.5f);

			bool active = _intensity > 0f || _rainbow > 0f;
			_render.SetFullRate(active);

			if (_intensity > 0f)
			{
				// Hop speed grows with typing speed.
				_phase += dt * Mathf.Lerp(7f, 13f, _intensity);
				float hop = Mathf.Abs(Mathf.Sin(_phase)) * _config.BounceHeight * _intensity;
				_motion.ZoomiesOffset = new Vector2(0f, hop);
				_motion.ZoomiesAngle = Mathf.Sin(_phase * 0.5f) * _config.WobbleDegrees * _intensity;
			}
			else
			{
				_phase = 0f;
				_motion.ZoomiesOffset = Vector2.zero;
				_motion.ZoomiesAngle = 0f;
			}

			UpdateTint(dt);
		}

		private void UpdateTint(float dt)
		{
			if (_tintTargets == null)
				return;

			if (_rainbow <= 0f)
			{
				RestoreTint();
				return;
			}

			if (!_tinted)
			{
				_originalColors = new Color[_tintTargets.Length];
				for (int i = 0; i < _tintTargets.Length; i++)
					_originalColors[i] = _tintTargets[i] != null ? _tintTargets[i].color : Color.white;
				_tinted = true;
			}

			_hue = (_hue + dt * 0.6f) % 1f;
			for (int i = 0; i < _tintTargets.Length; i++)
			{
				if (_tintTargets[i] == null)
					continue;

				// Offset hue per part so body and paw shimmer slightly apart.
				var rainbow = MathUtil.HsvToRgb((_hue + i * 0.08f) % 1f, 0.55f, 1f);
				var original = _originalColors[i];
				var tinted = Color.Lerp(original, original * rainbow, _rainbow * _config.RainbowStrength);
				tinted.a = original.a;
				_tintTargets[i].color = tinted;
			}
		}

		private void RestoreTint()
		{
			if (!_tinted)
				return;

			for (int i = 0; i < _tintTargets.Length; i++)
			{
				if (_tintTargets[i] != null)
					_tintTargets[i].color = _originalColors[i];
			}

			_tinted = false;
		}

		private void OnDisable()
		{
			RestoreTint();
			_intensity = 0f;
			_rainbow = 0f;
			if (_motion != null)
			{
				_motion.ZoomiesOffset = Vector2.zero;
				_motion.ZoomiesAngle = 0f;
			}
			_render.ReleaseAll();
		}
	}
}
