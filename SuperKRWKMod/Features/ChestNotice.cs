using BongoCat;
using SuperKRWKMod.Config;
using SuperKRWKMod.Debug;
using SuperKRWKMod.ModList;
using SuperKRWKMod.Utilities;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;

namespace SuperKRWKMod.Features
{
	/// <summary>
	/// Lets you know when a chest can be bought: a speech bubble above the cat and a shake of the
	/// shop button. Opening the chest is always left to you; this only reads the game's state
	/// (chest ready, price, whether the pets cover it) and never spends anything.
	/// </summary>
	public class ChestNotice : MonoBehaviour
	{
		// Used only when the game's chat bubble can't be found.
		private static readonly Vector2 FallbackPosition = new Vector2(0f, 142f);
		private const float CheckSeconds = 0.5f;
		private const float BubbleSeconds = 5f;
		private const float FadeSeconds = 0.6f;

		private class Watched
		{
			public Shop Shop;
			public bool IsEmote;
			public bool WasBuyable;
			public float LastNotice = -9999f;
		}

		private ModConfig _config;
		private RectTransform _bubble;
		private TMP_Text _bubbleText;
		private CanvasGroup _bubbleGroup;
		private Vector2 _bubbleBase;
		private readonly RenderDemand _render = new RenderDemand();
		private Watched _normal;
		private Watched _emote;
		private float _nextCheck;
		private float _bubbleTime = -100f;

		public void Init(ModConfig config, Transform mainCat, Transform fallbackParent, TMP_Text style)
		{
			_config = config;

			// A copy of the game's chat speech bubble, so it looks like the cat is talking. Only the
			// visuals are copied: the game's chat logic (which can send to multiplayer) isn't involved.
			var template = mainCat.Find("Chat/SpeechBubble");
			if (template != null)
			{
				var copy = Instantiate(template.gameObject, template.parent, false);
				copy.name = "SuperKRWKChestBubble";
				_bubble = (RectTransform)copy.transform;
				_bubbleText = copy.GetComponentInChildren<TMP_Text>(true);
				var localize = _bubbleText != null ? _bubbleText.GetComponent<LocalizeStringEvent>() : null;
				if (localize != null)
					Destroy(localize);
			}
			else
			{
				Log.WarnT(this, "Game speech bubble not found; using plain text.");
				var label = UiUtil.CreateLabel("SuperKRWKChestBubble", fallbackParent, style, 12f, FallbackPosition, new Vector2(240f, 30f));
				label.color = new Color(1f, 0.55f, 0.15f);
				_bubble = label.rectTransform;
				_bubbleText = label;
			}

			_bubbleGroup = _bubble.gameObject.GetComponent<CanvasGroup>();
			if (_bubbleGroup == null)
				_bubbleGroup = _bubble.gameObject.AddComponent<CanvasGroup>();
			// Never catch the mouse (CanvasGroup.blocksRaycasts is stripped, so per graphic).
			foreach (var graphic in _bubble.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
				graphic.raycastTarget = false;
			_bubbleBase = _bubble.anchoredPosition;
			_bubble.gameObject.SetActive(false);
		}

		private void Update()
		{
			AnimateBubble();

			if (Time.unscaledTime < _nextCheck)
				return;
			_nextCheck = Time.unscaledTime + CheckSeconds;

			if (_normal == null && Shop.NormalShop != null)
				_normal = new Watched { Shop = Shop.NormalShop };
			if (_emote == null && Shop.EmoteShop != null)
				_emote = new Watched { Shop = Shop.EmoteShop, IsEmote = true };

			Check(_normal);
			Check(_emote);
		}

		private void Check(Watched watched)
		{
			if (watched == null || watched.Shop == null || Pets.Instance == null)
				return;

			bool buyable = watched.Shop.CanGetChest;
			bool justBecameBuyable = buyable && !watched.WasBuyable;
			watched.WasBuyable = buyable;

			if (!buyable || !_config.ChestEnabled)
				return;

			float reminder = _config.ChestReminderMinutes * 60f;
			bool remind = reminder > 0f && Time.unscaledTime - watched.LastNotice >= reminder;
			if (!justBecameBuyable && !remind)
				return;

			watched.LastNotice = Time.unscaledTime;
			Notify(watched);
		}

		/// <summary>True while a chest is out in the shop (the only time a notice can appear).</summary>
		public static bool AnyChestOut =>
			(Shop.NormalShop != null && Shop.NormalShop.ChestIsReady) || (Shop.EmoteShop != null && Shop.EmoteShop.ChestIsReady);

		/// <summary>Shows the notice once for a chest that is out, to see what it looks like.</summary>
		public void Preview()
		{
			if (Shop.NormalShop != null && Shop.NormalShop.ChestIsReady)
				Notify(new Watched { Shop = Shop.NormalShop }, preview: true);
			else if (Shop.EmoteShop != null && Shop.EmoteShop.ChestIsReady)
				Notify(new Watched { Shop = Shop.EmoteShop, IsEmote = true }, preview: true);
		}

		private void Notify(Watched watched, bool preview = false)
		{
			Log.InfoT(this, preview ? "Chest notice preview." : $"{(watched.IsEmote ? "Emote chest" : "Chest")} can be bought.");

			if (_config.ChestBubble)
			{
				_bubbleText.text = watched.IsEmote
					? ModText.Get("Emote chest ready!", "エモート宝箱が買えるよ！")
					: ModText.Get("Chest ready!", "宝箱が買えるよ！");
				_bubbleTime = Time.unscaledTime;
				_bubble.gameObject.SetActive(true);
				_render.SetFullRate(true);
			}

			if (_config.ChestShake && watched.Shop != null)
			{
				// The game's own shake tween on the shop button: purely visual.
				var shake = watched.Shop.GetComponentInChildren<ShakeAnimation>(true);
				if (shake != null)
				{
					shake.Shake();
					OnDemandRenderHelper.Instance?.ResumeRenderingForDuration(1.5f);
				}
			}
		}

		private void AnimateBubble()
		{
			if (!_bubble.gameObject.activeSelf)
				return;

			float t = Time.unscaledTime - _bubbleTime;
			if (t >= BubbleSeconds || !_config.ChestEnabled)
			{
				_bubble.gameObject.SetActive(false);
				_render.SetFullRate(false);
				return;
			}

			// Pop in, bob gently, fade out at the end.
			float pop = Mathf.Clamp01(t / 0.2f);
			float scale = Mathf.Lerp(1.4f, 1f, pop * (2f - pop));
			_bubble.localScale = new Vector3(scale, scale, 1f);
			_bubble.anchoredPosition = _bubbleBase + new Vector2(0f, Mathf.Sin(t * 4f) * 2f);
			_bubbleGroup.alpha = 1f - Mathf.Clamp01((t - (BubbleSeconds - FadeSeconds)) / FadeSeconds);
		}

		/// <summary>Text for the Mods settings page.</summary>
		public string Describe()
		{
			var sb = new StringBuilder();
			sb.Append("<b>").Append(ModText.Get("Chests", "宝箱")).Append("</b>");
			AppendStatus(sb, Shop.NormalShop, ModText.Get("Chest", "宝箱"));
			AppendStatus(sb, Shop.EmoteShop, ModText.Get("Emote chest", "エモート宝箱"));
			return sb.ToString();
		}

		private static void AppendStatus(StringBuilder sb, Shop shop, string name)
		{
			if (shop == null)
				return;

			sb.Append('\n').Append(name).Append(": ");
			if (!shop.ChestIsReady)
			{
				int seconds = Mathf.Max(0, shop.StockRefreshTimeLeft);
				sb.Append(ModText.Get($"next in {seconds / 60}:{seconds % 60:00}", $"次まで {seconds / 60}分{seconds % 60:00}秒"));
				return;
			}

			if (shop.CanAffordChest)
			{
				sb.Append(ModText.Get("ready to buy!", "買えます！"));
				return;
			}

			var item = shop.GetComponentInChildren<ShopItem>(true);
			int missing = item != null ? MissingPets(item.Price) : -1;
			sb.Append(missing > 0
				? ModText.Get($"{missing:#,0} more taps", $"あと {missing:#,0} タップ")
				: ModText.Get("not enough taps yet", "タップ数が足りません"));
		}

		/// <summary>
		/// How many more pets are needed for <paramref name="price"/>. The game only exposes
		/// "can I spend N?", so this binary-searches it; nothing is spent.
		/// </summary>
		private static int MissingPets(int price)
		{
			var pets = Pets.Instance;
			if (pets == null || price <= 0)
				return -1;

			int low = 0, high = price; // largest affordable amount lies in [low, high]
			while (low < high)
			{
				int mid = (low + high + 1) / 2;
				if (pets.CanSpendPets(mid))
					low = mid;
				else
					high = mid - 1;
			}
			return price - low;
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
