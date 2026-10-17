using UnityEngine;

namespace SuperKRWKMod.Utilities
{
	/// <summary>
	/// Reads a sprite's pixels even when its texture isn't CPU-readable, by rendering it with a
	/// throwaway camera into a RenderTexture (Graphics.Blit is stripped from this game).
	/// </summary>
	public static class SpriteRasterizer
	{
		private const int Layer = 31;
		// tan(30 degrees): half of the default 60 degree vertical field of view.
		private const float DefaultHalfFovTan = 0.57735027f;
		private static readonly Vector3 FarAway = new Vector3(100000f, 100000f, 0f);

		/// <summary>Returns straight (non-premultiplied) RGBA pixels, bottom row first.</summary>
		public static Color[] Read(Sprite sprite, out int width, out int height)
		{
			width = (int)sprite.rect.width;
			height = (int)sprite.rect.height;
			float ppu = sprite.pixelsPerUnit;

			var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
			var root = new GameObject("SuperKRWKRasterizer");
			root.transform.position = FarAway;

			try
			{
				var spriteGo = new GameObject("Sprite");
				spriteGo.layer = Layer;
				spriteGo.transform.SetParent(root.transform, false);
				// Center the sprite's rect on the root regardless of its pivot.
				var centerFromPivot = (sprite.rect.size * 0.5f - sprite.pivot) / ppu;
				spriteGo.transform.localPosition = -(Vector3)centerFromPivot;
				var renderer = spriteGo.AddComponent<SpriteRenderer>();
				renderer.sprite = sprite;

				var camGo = new GameObject("Camera");
				camGo.transform.SetParent(root.transform, false);
				// Camera properties like orthographic/fieldOfView are stripped, so use the default
				// 60 degree perspective camera at the distance where the sprite exactly fills the view.
				// The sprite plane faces the camera, so there is no perspective distortion.
				float halfHeight = height / ppu * 0.5f;
				camGo.transform.localPosition = new Vector3(0f, 0f, -halfHeight / DefaultHalfFovTan);
				var cam = camGo.AddComponent<Camera>();
				cam.enabled = false;
				cam.clearFlags = CameraClearFlags.SolidColor;
				cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
				cam.cullingMask = 1 << Layer;
				cam.targetTexture = rt;
				cam.Render();

				var previous = RenderTexture.active;
				RenderTexture.active = rt;
				var readback = new Texture2D(width, height, TextureFormat.RGBA32, false);
				readback.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
				readback.Apply();
				RenderTexture.active = previous;
				cam.targetTexture = null;

				var pixels = readback.GetPixels(0, 0, width, height);
				Object.DestroyImmediate(readback);

				// Sprite shaders output premultiplied colour; undo it.
				for (int i = 0; i < pixels.Length; i++)
				{
					var p = pixels[i];
					if (p.a > 0.001f && p.a < 0.999f)
					{
						p.r = Mathf.Clamp01(p.r / p.a);
						p.g = Mathf.Clamp01(p.g / p.a);
						p.b = Mathf.Clamp01(p.b / p.a);
						pixels[i] = p;
					}
				}

				return pixels;
			}
			finally
			{
				// Immediately, so a second call in the same frame doesn't render this sprite too.
				Object.DestroyImmediate(root);
				Object.DestroyImmediate(rt);
			}
		}
	}
}
