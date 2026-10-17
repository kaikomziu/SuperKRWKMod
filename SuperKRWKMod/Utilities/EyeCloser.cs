using System.Collections.Generic;
using UnityEngine;

namespace SuperKRWKMod.Utilities
{
	/// <summary>
	/// Makes a copy of a cat body sprite with its eyes closed. Eyes are found as solid, roughly round
	/// dark blobs that don't touch the outline, so it works for any skin drawn in the game's style.
	/// </summary>
	public static class EyeCloser
	{
		private const float DarkLuminance = 0.45f;
		private const int MinEyePixels = 12;
		private const int MaxEyePixels = 2000;
		private const float MinFill = 0.6f;

		private struct Blob
		{
			public List<int> Pixels;
			public int MinX, MinY, MaxX, MaxY;
			public bool TouchesEdge;
			public int Width => MaxX - MinX + 1;
			public int Height => MaxY - MinY + 1;
			public float Fill => (float)Pixels.Count / (Width * Height);
		}

		/// <summary>Returns the closed-eye sprite, or null when no eyes could be found.</summary>
		public static Sprite Create(Sprite source, out int eyesFound)
		{
			var pixels = SpriteRasterizer.Read(source, out int w, out int h);
			var eyes = FindEyes(pixels, w, h);
			eyesFound = eyes.Count;
			if (eyes.Count == 0)
				return null;

			foreach (var eye in eyes)
				CloseEye(pixels, w, h, eye);

			var texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
			texture.filterMode = source.texture.filterMode;
			texture.SetPixels(pixels);
			texture.Apply();

			var pivot = new Vector2(source.pivot.x / w, source.pivot.y / h);
			var sprite = Sprite.Create(texture, new Rect(0f, 0f, w, h), pivot, source.pixelsPerUnit);
			sprite.name = source.name + "_SleepyEyes";
			return sprite;
		}

		private static float Luminance(Color c)
		{
			return 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
		}

		private static bool IsDark(Color c)
		{
			return c.a > 0.5f && Luminance(c) < DarkLuminance;
		}

		private static List<Blob> FindEyes(Color[] pixels, int w, int h)
		{
			var seen = new bool[pixels.Length];
			var stack = new Stack<int>();
			var candidates = new List<Blob>();

			for (int start = 0; start < pixels.Length; start++)
			{
				if (seen[start] || !IsDark(pixels[start]))
					continue;

				var blob = new Blob { Pixels = new List<int>(), MinX = w, MinY = h, MaxX = -1, MaxY = -1 };
				seen[start] = true;
				stack.Push(start);

				while (stack.Count > 0)
				{
					int i = stack.Pop();
					int x = i % w, y = i / w;
					blob.Pixels.Add(i);
					if (x < blob.MinX) blob.MinX = x;
					if (x > blob.MaxX) blob.MaxX = x;
					if (y < blob.MinY) blob.MinY = y;
					if (y > blob.MaxY) blob.MaxY = y;

					Visit(x + 1, y);
					Visit(x - 1, y);
					Visit(x, y + 1);
					Visit(x, y - 1);
				}

				var ratio = (float)blob.Width / blob.Height;
				if (!blob.TouchesEdge && blob.Pixels.Count >= MinEyePixels && blob.Pixels.Count <= MaxEyePixels
					&& ratio >= 0.6f && ratio <= 1.6f && blob.Fill >= MinFill)
				{
					candidates.Add(blob);
				}

				void Visit(int nx, int ny)
				{
					if (nx < 0 || ny < 0 || nx >= w || ny >= h || pixels[ny * w + nx].a < 0.5f)
					{
						// Next to transparency: part of the outline, not an eye.
						blob.TouchesEdge = true;
						return;
					}

					int n = ny * w + nx;
					if (!seen[n] && IsDark(pixels[n]))
					{
						seen[n] = true;
						stack.Push(n);
					}
				}
			}

			// The two biggest solid dots are the eyes (the mouth is a thin stroke and fails MinFill).
			candidates.Sort((a, b) => b.Pixels.Count.CompareTo(a.Pixels.Count));
			if (candidates.Count > 2)
				candidates.RemoveRange(2, candidates.Count - 2);
			return candidates;
		}

		private static void CloseEye(Color[] pixels, int w, int h, Blob eye)
		{
			float cx = eye.MinX + eye.Width * 0.5f;
			float cy = eye.MinY + eye.Height * 0.5f;
			float rx = eye.Width * 0.5f;
			float ry = eye.Height * 0.5f;
			float r = Mathf.Max(rx, ry);

			var ink = AverageColor(pixels, eye.Pixels);
			var skin = SurroundingColor(pixels, w, h, cx, cy, r);

			// 1. Paint the eye and its anti-aliased rim with the surrounding skin colour. Rim pixels are
			//    mixes of ink and skin; anything off that line (blush, other details) is left alone.
			int pad = 4;
			for (int y = Mathf.Max(0, eye.MinY - pad); y <= Mathf.Min(h - 1, eye.MaxY + pad); y++)
			{
				for (int x = Mathf.Max(0, eye.MinX - pad); x <= Mathf.Min(w - 1, eye.MaxX + pad); x++)
				{
					float ex = (x + 0.5f - cx) / (rx + 2.5f);
					float ey = (y + 0.5f - cy) / (ry + 2.5f);
					if (ex * ex + ey * ey > 1f)
						continue;

					int i = y * w + x;
					var p = pixels[i];
					if (p.a < 0.5f || !IsInkSkinMix(p, ink, skin))
						continue;

					pixels[i] = new Color(skin.r, skin.g, skin.b, p.a);
				}
			}

			// 2. Draw the closed eye: a soft downward curve, a little wider than the open eye.
			float halfWidth = rx * 1.25f;
			float sag = ry * 0.55f;
			float thickness = Mathf.Max(1.6f, ry * 0.5f);
			const int samples = 48;
			var curve = new Vector2[samples + 1];
			for (int s = 0; s <= samples; s++)
			{
				float t = s * 2f / samples - 1f;
				curve[s] = new Vector2(cx + halfWidth * t, cy + sag * 0.35f - sag * (1f - t * t));
			}

			int x0 = Mathf.Max(0, (int)(cx - halfWidth - thickness - 2f));
			int x1 = Mathf.Min(w - 1, (int)(cx + halfWidth + thickness + 2f));
			int y0 = Mathf.Max(0, (int)(cy - sag - thickness - 2f));
			int y1 = Mathf.Min(h - 1, (int)(cy + sag + thickness + 2f));

			for (int y = y0; y <= y1; y++)
			{
				for (int x = x0; x <= x1; x++)
				{
					var point = new Vector2(x + 0.5f, y + 0.5f);
					float best = float.MaxValue;
					foreach (var c in curve)
					{
						float d = (point - c).sqrMagnitude;
						if (d < best)
							best = d;
					}

					float coverage = Mathf.Clamp01(thickness * 0.5f + 0.5f - Mathf.Sqrt(best));
					if (coverage <= 0f)
						continue;

					int i = y * w + x;
					var p = pixels[i];
					var blended = Color.Lerp(p, ink, coverage);
					blended.a = Mathf.Max(p.a, coverage);
					pixels[i] = blended;
				}
			}
		}

		private static bool IsInkSkinMix(Color p, Color ink, Color skin)
		{
			var line = new Vector3(skin.r - ink.r, skin.g - ink.g, skin.b - ink.b);
			var rel = new Vector3(p.r - ink.r, p.g - ink.g, p.b - ink.b);
			float lengthSq = line.sqrMagnitude;
			if (lengthSq < 0.0001f)
				return false;

			float t = Vector3.Dot(rel, line) / lengthSq;
			if (t > 0.97f)
				return false; // already skin

			var closest = line * Mathf.Clamp01(t);
			return (rel - closest).magnitude < 0.08f;
		}

		private static Color AverageColor(Color[] pixels, List<int> indices)
		{
			float r = 0f, g = 0f, b = 0f;
			foreach (int i in indices)
			{
				r += pixels[i].r;
				g += pixels[i].g;
				b += pixels[i].b;
			}

			float n = indices.Count;
			return new Color(r / n, g / n, b / n, 1f);
		}

		/// <summary>Median-luminance opaque, non-dark colour in a ring just outside the eye.</summary>
		private static Color SurroundingColor(Color[] pixels, int w, int h, float cx, float cy, float r)
		{
			var ring = new List<Color>();
			int reach = (int)(r + 5f);
			for (int y = (int)cy - reach; y <= (int)cy + reach; y++)
			{
				for (int x = (int)cx - reach; x <= (int)cx + reach; x++)
				{
					if (x < 0 || y < 0 || x >= w || y >= h)
						continue;

					float dx = x + 0.5f - cx, dy = y + 0.5f - cy;
					float d = Mathf.Sqrt(dx * dx + dy * dy);
					var p = pixels[y * w + x];
					if (d >= r + 2f && d <= r + 4f && p.a > 0.9f && !IsDark(p))
						ring.Add(p);
				}
			}

			if (ring.Count == 0)
				return Color.white;

			ring.Sort((a, b) => Luminance(a).CompareTo(Luminance(b)));
			return ring[ring.Count / 2];
		}
	}
}
