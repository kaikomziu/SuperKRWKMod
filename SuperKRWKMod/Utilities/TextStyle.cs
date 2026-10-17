using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SuperKRWKMod.Utilities
{
	/// <summary>
	/// Shared look of the mod's on-screen text: size scale, outline and the background box.
	/// Setting the outline gives each label its own material instance, so the game's text is untouched.
	/// </summary>
	public static class TextStyle
	{
		private class Entry
		{
			public TMP_Text Label;
			public float BaseSize;
			public bool Boxed;
		}

		private static readonly List<Entry> Labels = new List<Entry>();
		private static readonly List<Image> Boxes = new List<Image>();
		private static bool _outline = true;
		private static Color _outlineColor = Color.white;
		private static float _outlineWidth = 0.35f;
		private static float _scale = 1.25f;
		private static bool _background = true;
		private static readonly int FaceDilate = Shader.PropertyToID("_FaceDilate");

		/// <summary>The game's counter frame; background boxes copy its sprite and colour.</summary>
		public static Image BoxTemplate { get; set; }

		public static void Register(TMP_Text label, float baseSize, bool boxed = false)
		{
			var entry = new Entry { Label = label, BaseSize = baseSize, Boxed = boxed };
			Labels.Add(entry);
			Apply(entry);
		}

		/// <summary>Changes a label's unscaled size (e.g. the meter's FontSize setting).</summary>
		public static void SetBaseSize(TMP_Text label, float baseSize)
		{
			foreach (var entry in Labels)
			{
				if (entry.Label == label)
				{
					entry.BaseSize = baseSize;
					Apply(entry);
				}
			}
		}

		public static void RegisterBox(Image box)
		{
			Boxes.Add(box);
			box.enabled = _background;
		}

		public static void Configure(bool outline, Color outlineColor, float outlineWidth, float scale, bool background)
		{
			_outline = outline;
			_outlineColor = outlineColor;
			_outlineWidth = Mathf.Clamp01(outlineWidth);
			_scale = Mathf.Clamp(scale, 0.5f, 3f);
			_background = background;

			Labels.RemoveAll(entry => entry.Label == null);
			foreach (var entry in Labels)
				Apply(entry);

			Boxes.RemoveAll(box => box == null);
			foreach (var box in Boxes)
				box.enabled = _background;
		}

		private static void Apply(Entry entry)
		{
			var label = entry.Label;
			if (label == null)
				return;

			label.fontSize = entry.BaseSize * _scale;

			// fontMaterial gives this label its own material instance (the game's shared one stays as is).
			// The outline is only drawn with the OUTLINE_ON keyword on.
			// Text in a background box is already readable; an outline there would only wash it out.
			bool outline = _outline && !(entry.Boxed && _background);
			var material = label.fontMaterial;
			if (outline)
				material.EnableKeyword("OUTLINE_ON");
			else
				material.DisableKeyword("OUTLINE_ON");

			label.outlineWidth = outline ? _outlineWidth : 0f;
			label.outlineColor = _outlineColor;
			// The outline is drawn half inside the glyph; grow the face so the text keeps its weight.
			material.SetFloat(FaceDilate, outline ? _outlineWidth * 0.5f : 0f);
		}
	}
}
