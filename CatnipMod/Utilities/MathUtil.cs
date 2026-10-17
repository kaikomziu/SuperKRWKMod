using UnityEngine;

namespace CatnipMod.Utilities
{
	/// <summary>
	/// Replacements for Mathf/Color helpers that are stripped from the game's UnityEngine assemblies.
	/// </summary>
	public static class MathUtil
	{
		public const float PI = 3.14159265f;

		/// <summary>
		/// e^x via (1 + x/256)^256 (System.Math.Exp is stripped too). Accurate enough for the small
		/// per-frame smoothing exponents used here.
		/// </summary>
		public static float Exp(float value)
		{
			float result = 1f + value / 256f;
			if (result <= 0f)
				return 0f;

			for (int i = 0; i < 8; i++)
				result *= result;

			return result;
		}

		public static float MoveTowards(float current, float target, float maxDelta)
		{
			if (System.Math.Abs(target - current) <= maxDelta)
				return target;

			return current + (target > current ? maxDelta : -maxDelta);
		}

		/// <summary>Hue, saturation, value in 0..1 to an opaque RGB color.</summary>
		public static Color HsvToRgb(float h, float s, float v)
		{
			h = (h % 1f + 1f) % 1f * 6f;
			int sector = (int)h;
			float f = h - sector;
			float p = v * (1f - s);
			float q = v * (1f - s * f);
			float t = v * (1f - s * (1f - f));

			switch (sector)
			{
				case 0: return new Color(v, t, p, 1f);
				case 1: return new Color(q, v, p, 1f);
				case 2: return new Color(p, v, t, 1f);
				case 3: return new Color(p, q, v, 1f);
				case 4: return new Color(t, p, v, 1f);
				default: return new Color(v, p, q, 1f);
			}
		}
	}
}
