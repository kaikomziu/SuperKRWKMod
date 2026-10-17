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
	/// Closes the cat's eyes while it sleeps by swapping the body image for a generated copy.
	/// </summary>
	public class SleepyEyes : MonoBehaviour
	{
		private ModConfig _config;
		private SleepMode _sleep;
		private Image _body;

		// Original sprite -> closed-eye copy (null when no eyes were found, so it isn't retried).
		private readonly Dictionary<Sprite, Sprite> _closedBySprite = new Dictionary<Sprite, Sprite>();
		private readonly Dictionary<Sprite, Sprite> _originalByClosed = new Dictionary<Sprite, Sprite>();

		public void Init(ModConfig config, SleepMode sleep, Image body)
		{
			_config = config;
			_sleep = sleep;
			_body = body;
		}

		private void LateUpdate()
		{
			if (_body == null)
				return;

			var current = _body.sprite;
			if (current == null)
				return;

			bool showingClosed = _originalByClosed.TryGetValue(current, out var original);
			bool wantClosed = _sleep.IsAsleep && _config.SleepCloseEyes;

			if (wantClosed && !showingClosed)
			{
				var closed = GetClosed(current);
				if (closed != null)
					Swap(closed);
			}
			else if (!wantClosed && showingClosed)
			{
				Swap(original);
			}
		}

		private Sprite GetClosed(Sprite sprite)
		{
			if (_closedBySprite.TryGetValue(sprite, out var closed))
				return closed;

			try
			{
				closed = EyeCloser.Create(sprite, out int eyes);
				Log.InfoT(this, closed != null
					? $"Generated sleepy eyes for {sprite.name} ({eyes} eye(s))."
					: $"No eyes found on {sprite.name}; leaving it as is.");
			}
			catch (Exception e)
			{
				Log.ExceptionT(this, e, $"Failed to close eyes on {sprite.name}");
				closed = null;
			}

			_closedBySprite[sprite] = closed;
			if (closed != null)
				_originalByClosed[closed] = sprite;
			return closed;
		}

		private void Swap(Sprite sprite)
		{
			_body.sprite = sprite;
			// The game may have paused rendering in the background; show the change.
			OnDemandRenderHelper.Instance?.ResumeRenderingForDuration(0.05f);
		}

		private void OnDisable()
		{
			if (_body != null && _body.sprite != null && _originalByClosed.TryGetValue(_body.sprite, out var original))
				_body.sprite = original;
		}
	}
}
