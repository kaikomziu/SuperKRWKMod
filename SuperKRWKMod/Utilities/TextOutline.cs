using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace SuperKRWKMod.Utilities
{
	/// <summary>
	/// Outline on the mod's on-screen labels so they stay readable on any background.
	/// Setting the outline gives each label its own material instance, so the game's text is untouched.
	/// </summary>
	public static class TextOutline
	{
		private static readonly List<TMP_Text> Labels = new List<TMP_Text>();
		private static bool _enabled = true;
		private static Color _color = Color.white;
		private static float _width = 0.25f;
		private static bool _loggedShader;

		public static void Register(TMP_Text label)
		{
			Labels.Add(label);
			Apply(label);
		}

		public static void Configure(bool enabled, Color color, float width)
		{
			_enabled = enabled;
			_color = color;
			_width = Mathf.Clamp01(width);

			Labels.RemoveAll(label => label == null);
			foreach (var label in Labels)
				Apply(label);
		}

		private static void Apply(TMP_Text label)
		{
			if (label == null)
				return;

			// fontMaterial gives this label its own material instance (the game's shared one stays as is).
			var material = label.fontMaterial;
			if (!_loggedShader)
			{
				_loggedShader = true;
				global::SuperKRWKMod.Debug.Log.Info($"Label shader: {material.shader.name}");
			}

			// The mobile SDF shader only draws the outline with this keyword on.
			if (_enabled)
				material.EnableKeyword("OUTLINE_ON");
			else
				material.DisableKeyword("OUTLINE_ON");

			label.outlineWidth = _enabled ? _width : 0f;
			label.outlineColor = _color;
		}
	}
}
