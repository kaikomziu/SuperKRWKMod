using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SuperKRWKMod.Utilities
{
	public static class UiUtil
	{
		/// <summary>
		/// Creates a non-interactive TextMeshPro label styled like <paramref name="style"/> (if given).
		/// Its size follows the shared text scale (see <see cref="TextStyle"/>).
		/// </summary>
		public static TextMeshProUGUI CreateLabel(string name, Transform parent, TMP_Text style, float fontSize, Vector2 anchoredPosition, Vector2 size)
		{
			return CreateLabel(name, parent, style, fontSize, anchoredPosition, size, boxed: false);
		}

		private static TextMeshProUGUI CreateLabel(string name, Transform parent, TMP_Text style, float fontSize, Vector2 anchoredPosition, Vector2 size, bool boxed)
		{
			var go = new GameObject(name);
			go.layer = parent.gameObject.layer;

			var rect = go.AddComponent<RectTransform>();
			rect.SetParent(parent, false);
			rect.anchorMin = new Vector2(0.5f, 0.5f);
			rect.anchorMax = new Vector2(0.5f, 0.5f);
			rect.pivot = new Vector2(0.5f, 0.5f);
			rect.sizeDelta = size;
			rect.anchoredPosition = anchoredPosition;

			var label = go.AddComponent<TextMeshProUGUI>();
			if (style != null)
			{
				label.font = style.font;
				label.fontSharedMaterial = style.fontSharedMaterial;
				label.color = style.color;
			}

			label.alignment = TextAlignmentOptions.Center;
			label.textWrappingMode = TextWrappingModes.NoWrap;
			label.richText = true;
			label.raycastTarget = false;
			label.text = "";
			TextStyle.Register(label, fontSize, boxed);

			return label;
		}

		/// <summary>
		/// A label inside a background box that looks like the game's counter frame and fits the text.
		/// Move, show or hide the box (<see cref="BoxOf"/>), not the label.
		/// </summary>
		/// <param name="pivot">Which point of the box sits at <paramref name="anchoredPosition"/>.</param>
		public static TextMeshProUGUI CreateBoxedLabel(string name, Transform parent, TMP_Text style, float fontSize, Vector2 anchoredPosition, Vector2 pivot)
		{
			var box = new GameObject(name + "Box");
			box.layer = parent.gameObject.layer;

			var rect = box.AddComponent<RectTransform>();
			rect.SetParent(parent, false);
			rect.anchorMin = new Vector2(0.5f, 0.5f);
			rect.anchorMax = new Vector2(0.5f, 0.5f);
			rect.pivot = pivot;
			rect.anchoredPosition = anchoredPosition;

			var image = box.AddComponent<Image>();
			image.raycastTarget = false;
			var template = TextStyle.BoxTemplate;
			if (template != null)
			{
				image.sprite = template.sprite;
				image.type = template.type;
				image.color = template.color;
			}
			else
			{
				image.color = new Color(1f, 1f, 1f, 0.85f);
			}
			TextStyle.RegisterBox(image);

			var layout = box.AddComponent<HorizontalLayoutGroup>();
			layout.padding = new RectOffset(6, 6, 2, 2);
			layout.childAlignment = TextAnchor.MiddleCenter;
			layout.childControlWidth = true;
			layout.childControlHeight = true;
			layout.childForceExpandWidth = false;
			layout.childForceExpandHeight = false;

			var fitter = box.AddComponent<ContentSizeFitter>();
			fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
			fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

			return CreateLabel(name, box.transform, style, fontSize, Vector2.zero, Vector2.zero, boxed: true);
		}

		/// <summary>The box around a label made by <see cref="CreateBoxedLabel"/>, else the label itself.</summary>
		public static RectTransform BoxOf(TMP_Text label)
		{
			var parent = label.rectTransform.parent as RectTransform;
			return parent != null && parent.name == label.name + "Box" ? parent : label.rectTransform;
		}
	}
}
