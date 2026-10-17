using BongoCat;
using UnityEngine.Rendering;

namespace SuperKRWKMod.Utilities
{
	/// <summary>
	/// Cooperates with the game's OnDemandRenderHelper, which stops rendering while the game is
	/// unfocused and idle. Each user holds at most one full-rate request, always released in pairs.
	/// </summary>
	public class RenderDemand
	{
		// Value OnDemandRenderHelper uses to effectively pause rendering.
		private const int PausedInterval = 100000;

		private bool _fullRate;
		private int _lowRateInterval;

		/// <summary>Request (or release) rendering at the full frame rate.</summary>
		public void SetFullRate(bool wanted)
		{
			var helper = OnDemandRenderHelper.Instance;
			if (wanted == _fullRate || helper == null)
				return;

			_fullRate = wanted;
			if (wanted)
				helper.ResumeRendering();
			else
				helper.TryPauseRendering();
		}

		/// <summary>
		/// While set, lifts a paused game to roughly <paramref name="fps"/> frames per second. Call every frame.
		/// Pass 0 to release.
		/// </summary>
		public void SetLowRate(int fps)
		{
			if (fps <= 0)
			{
				// Only put the pause back if nobody else changed the interval meanwhile.
				if (_lowRateInterval > 0 && OnDemandRendering.renderFrameInterval == _lowRateInterval)
					OnDemandRendering.renderFrameInterval = PausedInterval;

				_lowRateInterval = 0;
				return;
			}

			int interval = System.Math.Max(1, 60 / fps);
			if (OnDemandRendering.renderFrameInterval > interval)
			{
				OnDemandRendering.renderFrameInterval = interval;
				_lowRateInterval = interval;
			}
		}

		public void ReleaseAll()
		{
			SetFullRate(false);
			SetLowRate(0);
		}
	}
}
