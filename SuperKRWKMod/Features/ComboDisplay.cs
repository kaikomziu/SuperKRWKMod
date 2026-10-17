using SuperKRWKMod.Config;
using SuperKRWKMod.Utilities;
using TMPro;
using UnityEngine;

namespace SuperKRWKMod.Features
{
	/// <summary>
	/// "×128 COMBO" next to the cat while you keep typing without a pause. It pops on every tap,
	/// changes colour at milestones, and lingers briefly after the combo ends.
	/// </summary>
	public class ComboDisplay : MonoBehaviour, IMovableText
	{
		private static readonly Vector2 BasePosition = new Vector2(-62f, 82f);
		private const float PopScale = 1.35f;
		private const float PopSeconds = 0.15f;
		private const float HoldSeconds = 0.8f;
		private const float FadeSeconds = 0.5f;

		private ModConfig _config;
		private TapTracker _tracker;
		private TextMeshProUGUI _label;
		private Color _baseColor;
		private readonly RenderDemand _render = new RenderDemand();

		private int _shown;
		private float _popTime = -10f;
		private float _endTime = -10f;
		private float _hue;
		private bool _editing;

		public void Init(ModConfig config, TapTracker tracker, Transform parent, TMP_Text style)
		{
			_config = config;
			_tracker = tracker;
			_label = UiUtil.CreateLabel("SuperKRWKCombo", parent, style, 13f, BasePosition, new Vector2(90f, 40f));
			_baseColor = _label.color;
			_label.gameObject.SetActive(false);
			ApplyConfig();
		}

		public void ApplyConfig()
		{
			if (_label == null)
				return;

			_label.rectTransform.anchoredPosition = BasePosition + new Vector2(_config.ComboOffsetX, _config.ComboOffsetY);
			if (!_config.ComboEnabled)
				Hide();
		}

		private void Update()
		{
			if (_label == null || _editing)
				return;

			float now = Time.unscaledTime;
			int combo = _tracker.Combo;
			bool enabled = _config.ComboEnabled;

			if (enabled && combo >= _config.ComboMin)
			{
				if (combo != _shown)
				{
					_shown = combo;
					_popTime = now;
					_label.text = $"<b>×{combo}</b>\n<size=55%>COMBO</size>";
				}

				_endTime = -10f;
				Show();
			}
			else if (_label.gameObject.activeSelf && _endTime < 0f)
			{
				// The combo just ended (or dropped below the minimum): hold the final count, then fade.
				_endTime = now;
			}

			if (!_label.gameObject.activeSelf)
				return;

			float alpha = 1f;
			if (_endTime >= 0f)
			{
				float since = now - _endTime;
				alpha = 1f - Mathf.Clamp01((since - HoldSeconds) / FadeSeconds);
				if (alpha <= 0f || !enabled)
				{
					Hide();
					return;
				}
			}

			float pop = Mathf.Clamp01((now - _popTime) / PopSeconds);
			float scale = Mathf.Lerp(PopScale, 1f, pop * (2f - pop));
			_label.rectTransform.localScale = new Vector3(scale, scale, 1f);

			var color = TierColor(_shown);
			color.a = alpha;
			_label.color = color;
		}

		private Color TierColor(int combo)
		{
			if (combo >= 200)
			{
				_hue = (_hue + Time.unscaledDeltaTime * 0.8f) % 1f;
				return MathUtil.HsvToRgb(_hue, 0.6f, 1f);
			}

			if (combo >= 100)
				return new Color(1f, 0.42f, 0.62f); // pink
			if (combo >= 50)
				return new Color(1f, 0.62f, 0.2f); // orange
			return _baseColor;
		}

		private void Show()
		{
			if (_label.gameObject.activeSelf)
				return;

			_label.gameObject.SetActive(true);
			_render.SetFullRate(true);
		}

		private void Hide()
		{
			_label.gameObject.SetActive(false);
			_shown = 0;
			_endTime = -10f;
			_render.SetFullRate(false);
		}

		// --- IMovableText (layout editor) ---------------------------------------------------------

		public RectTransform LayoutTarget => _label.rectTransform;
		public Vector2 LayoutBase => BasePosition;
		public string LayoutSection => "Combo";
		public string LayoutKeyX => "OffsetX";
		public string LayoutKeyY => "OffsetY";

		public void SetEditing(bool editing)
		{
			_editing = editing;
			if (editing)
			{
				// A sample, since there is no combo while you're dragging things around.
				_label.text = "<b>×123</b>\n<size=55%>COMBO</size>";
				_label.rectTransform.localScale = Vector3.one;
				_label.color = _baseColor;
				_label.gameObject.SetActive(true);
			}
			else
			{
				Hide();
				ApplyConfig();
			}
		}

		private void OnDisable()
		{
			_render.ReleaseAll();
		}

		private void OnDestroy()
		{
			if (_label != null)
				Destroy(_label.gameObject);
		}
	}
}
