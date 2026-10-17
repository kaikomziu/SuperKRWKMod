using BongoCat;
using CatnipMod.Config;
using CatnipMod.Utilities;
using TMPro;
using UnityEngine;

namespace CatnipMod.Features
{
	/// <summary>
	/// Small typing speed readout above the cat: current KPM, session best and session taps.
	/// </summary>
	public class SpeedMeter : MonoBehaviour
	{
		private const float RefreshInterval = 0.25f;
		private static readonly Vector2 BasePosition = new Vector2(0f, 112f);

		private CatnipConfig _config;
		private TapTracker _tracker;
		private TextMeshProUGUI _label;
		private float _nextRefresh;
		private string _lastText;

		public void Init(CatnipConfig config, TapTracker tracker, Transform parent, TMP_Text style)
		{
			_config = config;
			_tracker = tracker;
			_label = UiUtil.CreateLabel("CatnipSpeedMeter", parent, style, config.MeterFontSize, BasePosition, new Vector2(220f, 40f));
			ApplyConfig();
		}

		public void ApplyConfig()
		{
			if (_label == null)
				return;

			_label.fontSize = _config.MeterFontSize;
			_label.rectTransform.anchoredPosition = BasePosition + new Vector2(_config.MeterOffsetX, _config.MeterOffsetY);
			_label.gameObject.SetActive(_config.MeterEnabled && _config.MeterVisible);
			_lastText = null;
			RequestFrame();
		}

		private void Update()
		{
			if (_label == null || !_config.MeterEnabled)
				return;

			if (Input.GetKeyDown(_config.MeterToggleKey))
			{
				bool visible = !_label.gameObject.activeSelf;
				_config.Set("Meter", "Visible", visible ? "true" : "false");
				_label.gameObject.SetActive(visible);
				RequestFrame();
			}

			if (!_label.gameObject.activeSelf || Time.unscaledTime < _nextRefresh)
				return;

			_nextRefresh = Time.unscaledTime + RefreshInterval;

			int kpm = Mathf.RoundToInt(_tracker.Kpm);
			int best = Mathf.RoundToInt(_tracker.BestKpm);
			var text = $"<b>{kpm}</b> <size=70%>KPM</size>\n<size=65%>best {best} | {_tracker.SessionTaps:#,0} taps</size>";

			if (text != _lastText)
			{
				_label.text = text;
				_lastText = text;
				RequestFrame();
			}
		}

		private static void RequestFrame()
		{
			// The game may have paused rendering while in the background; show the new text.
			OnDemandRenderHelper.Instance?.ResumeRenderingForDuration(0.05f);
		}

		private void OnDestroy()
		{
			if (_label != null)
				Destroy(_label.gameObject);
		}
	}
}
