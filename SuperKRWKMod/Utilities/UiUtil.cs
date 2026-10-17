using TMPro;
using UnityEngine;

namespace SuperKRWKMod.Utilities
{
	public static class UiUtil
	{
		/// <summary>
		/// Creates a non-interactive TextMeshPro label styled like <paramref name="style"/> (if given).
		/// </summary>
		public static TextMeshProUGUI CreateLabel(string name, Transform parent, TMP_Text style, float fontSize, Vector2 anchoredPosition, Vector2 size)
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

			label.fontSize = fontSize;
			label.alignment = TextAlignmentOptions.Center;
			label.textWrappingMode = TextWrappingModes.NoWrap;
			label.richText = true;
			label.raycastTarget = false;
			label.text = "";
			TextOutline.Register(label);

			return label;
		}
	}
}
