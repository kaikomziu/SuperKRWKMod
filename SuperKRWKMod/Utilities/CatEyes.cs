using System.Collections.Generic;
using UnityEngine;

namespace SuperKRWKMod.Utilities
{
	/// <summary>
	/// Finds the eyes on a cat sprite and makes variants of it: eyes closed, or pupils shifted to look
	/// in a direction. Eyes are solid, roughly round dark blobs that don't touch the outline, so this
	/// works for any skin drawn in the game's style.
	/// </summary>
	public class CatEyes
	{
		private const float DarkLuminance = 0.45f;
		private const int MinEyePixels = 12;
		private const int MaxEyePixels = 2000;
		private const float MinFill = 0.6f;
		private const float RimPadding = 2.5f;

		private class Eye
		{
			public float Cx, Cy, Rx, Ry;
			public Color Ink, Skin;
			// Ink coverage (0..1) of the original eye over a padded box, used to redraw it elsewhere.
			public int BoxX, BoxY, BoxW, BoxH;
			public float[] Coverage;
		}

		private readonly Sprite _source;
		private readonly int _w, _h;
		private readonly Color[] _blank; // the sprite with its eyes painted over with skin colour
		private readonly List<Eye> _eyes;

		public int EyeCount => _eyes.Count;

		private CatEyes(Sprite source, int w, int h, Color[] blank, List<Eye> eyes)
		{
			_source = source;
			_w = w;
			_h = h;
			_blank = blank;
			_eyes = eyes;
		}

		/// <summary>Returns null when no eyes could be found.</summary>
		public static CatEyes Analyze(Sprite source)
		{
			var pixels = SpriteRasterizer.Read(source, out int w, out int h);
			var blobs = FindEyeBlobs(pixels, w, h);
			if (blobs.Count == 0)
				return null;

			var eyes = new List<Eye>();
			foreach (var blob in blobs)
				eyes.Add(MeasureEye(pixels, w, h, blob));

			// Erase after measuring all eyes so measurements see the original art.
			foreach (var eye in eyes)
				Erase(pixels, w, eye);

			return new CatEyes(source, w, h, pixels, eyes);
		}

		public Sprite MakeClosed()
		{
			var pixels = (Color[])_blank.Clone();
			foreach (var eye in _eyes)
				DrawClosed(pixels, eye);
			return ToSprite(pixels, "_Closed");
		}

		/// <summary>Pupils moved by (dx, dy) sprite pixels; +y is up.</summary>
		public Sprite MakeLooking(int dx, int dy)
		{
			var pixels = (Color[])_blank.Clone();
			foreach (var eye in _eyes)
				DrawShifted(pixels, eye, dx, dy);
			return ToSprite(pixels, $"_Look{dx}_{dy}");
		}

		private Sprite ToSprite(Color[] pixels, string suffix)
		{
			var texture = new Texture2D(_w, _h, TextureFormat.RGBA32, false);
			texture.filterMode = _source.texture.filterMode;
			texture.SetPixels(pixels);
			texture.Apply();

			var pivot = new Vector2(_source.pivot.x / _w, _source.pivot.y / _h);
			var sprite = Sprite.Create(texture, new Rect(0f, 0f, _w, _h), pivot, _source.pixelsPerUnit);
			sprite.name = _source.name + suffix;
			return sprite;
		}

		// --- Detection ---------------------------------------------------------------------------

		private struct Blob
		{
			public List<int> Pixels;
			public int MinX, MinY, MaxX, MaxY;
			public bool TouchesEdge;
			public int Width => MaxX - MinX + 1;
			public int Height => MaxY - MinY + 1;
			public float Fill => (float)Pixels.Count / (Width * Height);
		}

		private static float Luminance(Color c)
		{
			return 0.299f * c.r + 0.587f * c.g + 0.114f * c.b;
		}

		private static bool IsDark(Color c)
		{
			return c.a > 0.5f && Luminance(c) < DarkLuminance;
		}

		private static List<Blob> FindEyeBlobs(Color[] pixels, int w, int h)
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

		private static Eye MeasureEye(Color[] pixels, int w, int h, Blob blob)
		{
			var eye = new Eye
			{
				Cx = blob.MinX + blob.Width * 0.5f,
				Cy = blob.MinY + blob.Height * 0.5f,
				Rx = blob.Width * 0.5f,
				Ry = blob.Height * 0.5f,
				Ink = AverageColor(pixels, blob.Pixels),
			};
			eye.Skin = SurroundingColor(pixels, w, h, eye.Cx, eye.Cy, Mathf.Max(eye.Rx, eye.Ry));

			int pad = 4;
			eye.BoxX = Mathf.Max(0, blob.MinX - pad);
			eye.BoxY = Mathf.Max(0, blob.MinY - pad);
			eye.BoxW = Mathf.Min(w - 1, blob.MaxX + pad) - eye.BoxX + 1;
			eye.BoxH = Mathf.Min(h - 1, blob.MaxY + pad) - eye.BoxY + 1;
			eye.Coverage = new float[eye.BoxW * eye.BoxH];

			for (int by = 0; by < eye.BoxH; by++)
			{
				for (int bx = 0; bx < eye.BoxW; bx++)
				{
					int x = eye.BoxX + bx, y = eye.BoxY + by;
					if (!InRim(eye, x, y))
						continue;

					var p = pixels[y * w + x];
					if (p.a >= 0.5f && InkAmount(p, eye.Ink, eye.Skin, out float ink))
						eye.Coverage[by * eye.BoxW + bx] = ink;
				}
			}

			return eye;
		}

		private static bool InRim(Eye eye, int x, int y)
		{
			float ex = (x + 0.5f - eye.Cx) / (eye.Rx + RimPadding);
			float ey = (y + 0.5f - eye.Cy) / (eye.Ry + RimPadding);
			return ex * ex + ey * ey <= 1f;
		}

		/// <summary>
		/// For a pixel on the line between ink and skin colour, how much ink it holds. Pixels off that
		/// line (blush, other details) return false so they are never touched.
		/// </summary>
		private static bool InkAmount(Color p, Color ink, Color skin, out float amount)
		{
			amount = 0f;
			var line = new Vector3(skin.r - ink.r, skin.g - ink.g, skin.b - ink.b);
			var rel = new Vector3(p.r - ink.r, p.g - ink.g, p.b - ink.b);
			float lengthSq = line.sqrMagnitude;
			if (lengthSq < 0.0001f)
				return false;

			float t = Vector3.Dot(rel, line) / lengthSq;
			if (t > 0.97f)
				return false; // already skin

			var closest = line * Mathf.Clamp01(t);
			if ((rel - closest).magnitude >= 0.08f)
				return false;

			amount = 1f - Mathf.Clamp01(t);
			return true;
		}

		// --- Drawing -------------------------------------------------------------------------------

		private static void Erase(Color[] pixels, int w, Eye eye)
		{
			for (int by = 0; by < eye.BoxH; by++)
			{
				for (int bx = 0; bx < eye.BoxW; bx++)
				{
					if (eye.Coverage[by * eye.BoxW + bx] <= 0f)
						continue;

					int i = (eye.BoxY + by) * w + eye.BoxX + bx;
					pixels[i] = new Color(eye.Skin.r, eye.Skin.g, eye.Skin.b, pixels[i].a);
				}
			}
		}

		private void DrawShifted(Color[] pixels, Eye eye, int dx, int dy)
		{
			for (int by = 0; by < eye.BoxH; by++)
			{
				for (int bx = 0; bx < eye.BoxW; bx++)
				{
					float ink = eye.Coverage[by * eye.BoxW + bx];
					if (ink <= 0f)
						continue;

					int x = eye.BoxX + bx + dx, y = eye.BoxY + by + dy;
					if (x < 0 || y < 0 || x >= _w || y >= _h)
						continue;

					int i = y * _w + x;
					var p = pixels[i];
					if (p.a < 0.5f)
						continue; // never draw outside the body

					var blended = Color.Lerp(p, eye.Ink, ink);
					blended.a = p.a;
					pixels[i] = blended;
				}
			}
		}

		private void DrawClosed(Color[] pixels, Eye eye)
		{
			// A soft downward curve, a little wider than the open eye.
			float halfWidth = eye.Rx * 1.25f;
			float sag = eye.Ry * 0.55f;
			float thickness = Mathf.Max(1.6f, eye.Ry * 0.5f);
			const int samples = 48;
			var curve = new Vector2[samples + 1];
			for (int s = 0; s <= samples; s++)
			{
				float t = s * 2f / samples - 1f;
				curve[s] = new Vector2(eye.Cx + halfWidth * t, eye.Cy + sag * 0.35f - sag * (1f - t * t));
			}

			int x0 = Mathf.Max(0, (int)(eye.Cx - halfWidth - thickness - 2f));
			int x1 = Mathf.Min(_w - 1, (int)(eye.Cx + halfWidth + thickness + 2f));
			int y0 = Mathf.Max(0, (int)(eye.Cy - sag - thickness - 2f));
			int y1 = Mathf.Min(_h - 1, (int)(eye.Cy + sag + thickness + 2f));

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

					int i = y * _w + x;
					var p = pixels[i];
					var blended = Color.Lerp(p, eye.Ink, coverage);
					blended.a = Mathf.Max(p.a, coverage);
					pixels[i] = blended;
				}
			}
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
