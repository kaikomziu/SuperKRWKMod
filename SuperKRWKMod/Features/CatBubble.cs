using SuperKRWKMod.Debug;
using SuperKRWKMod.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;

namespace SuperKRWKMod.Features
{
	/// <summary>
	/// The cat "says" something in a copy of the game's chat speech bubble. Only the visuals are copied:
	/// the game's chat logic (which can send to multiplayer) isn't involved, so nothing is sent.
	/// Shared by the chest notice, break reminder and greetings.
	/// </summary>
	public class CatBubble : MonoBehaviour
	{
		// Used only when the game's chat bubble can't be found.
		private static readonly Vector2 FallbackPosition = new Vector2(0f, 142f);
		private const float FadeSeconds = 0.6f;

		private RectTransform _bubble;
		private TMP_Text _text;
		private CanvasGroup _group;
		private Vector2 _base;
		private readonly RenderDemand _render = new RenderDemand();
		private float _shownAt = -100f;
		private float _duration;

		public bool IsShowing => _bubble != null && _bubble.gameObject.activeSelf;

		public void Init(Transform mainCat, Transform fallbackParent, TMP_Text style)
		{
			var template = mainCat.Find("Chat/SpeechBubble");
			if (template != null)
			{
				var copy = Instantiate(template.gameObject, template.parent, false);
				copy.name = "SuperKRWKBubble";
				_bubble = (RectTransform)copy.transform;
				_text = copy.GetComponentInChildren<TMP_Text>(true);
				var localize = _text != null ? _text.GetComponent<LocalizeStringEvent>() : null;
				if (localize != null)
					Destroy(localize);
			}
			else
			{
				Log.WarnT(this, "Game speech bubble not found; using plain text.");
				var label = UiUtil.CreateLabel("SuperKRWKBubble", fallbackParent, style, 12f, FallbackPosition, new Vector2(240f, 30f));
				label.color = new Color(1f, 0.55f, 0.15f);
				_bubble = label.rectTransform;
				_text = label;
			}

			_group = _bubble.gameObject.GetComponent<CanvasGroup>();
			if (_group == null)
				_group = _bubble.gameObject.AddComponent<CanvasGroup>();
			// Never catch the mouse (CanvasGroup.blocksRaycasts is stripped, so per graphic).
			foreach (var graphic in _bubble.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
				graphic.raycastTarget = false;
			_base = _bubble.anchoredPosition;
			_bubble.gameObject.SetActive(false);
		}

		/// <summary>Shows <paramref name="text"/> for <paramref name="seconds"/>, replacing anything showing.</summary>
		public void Say(string text, float seconds = 5f)
		{
			if (_bubble == null)
				return;

			Log.InfoT(this, "Says: " + text);
			_text.text = text;
			_shownAt = Time.unscaledTime;
			_duration = Mathf.Max(1f, seconds);
			_bubble.gameObject.SetActive(true);
			_render.SetFullRate(true);
		}

		public void Hide()
		{
			if (!IsShowing)
				return;

			_bubble.gameObject.SetActive(false);
			_render.SetFullRate(false);
		}

		private void Update()
		{
			if (!IsShowing)
				return;

			float t = Time.unscaledTime - _shownAt;
			if (t >= _duration)
			{
				Hide();
				return;
			}

			// Pop in, bob gently, fade out at the end.
			float pop = Mathf.Clamp01(t / 0.2f);
			float scale = Mathf.Lerp(1.4f, 1f, pop * (2f - pop));
			_bubble.localScale = new Vector3(scale, scale, 1f);
			_bubble.anchoredPosition = _base + new Vector2(0f, Mathf.Sin(t * 4f) * 2f);
			_group.alpha = 1f - Mathf.Clamp01((t - (_duration - FadeSeconds)) / FadeSeconds);
		}

		private void OnDisable()
		{
			_render.ReleaseAll();
		}

		private void OnDestroy()
		{
			if (_bubble != null)
				Destroy(_bubble.gameObject);
		}
	}
}
