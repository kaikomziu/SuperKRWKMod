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
		// Left of the combo counter (see ComboDisplay).
		private static readonly Vector2 CountdownPosition = new Vector2(-155f, 82f);
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
		private TextMeshProUGUI _countdown;
		private string _countdownText;
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

			_countdown = UiUtil.CreateLabel("SuperKRWKChestCountdown", fallbackParent, style, 9f, CountdownPosition, new Vector2(120f, 30f));
			_countdown.alignment = TextAlignmentOptions.Right;
			_countdown.gameObject.SetActive(false);
			ApplyConfig();
		}

		public void ApplyConfig()
		{
			if (_countdown != null)
				_countdown.rectTransform.anchoredPosition = CountdownPosition + new Vector2(_config.CountdownOffsetX, _config.CountdownOffsetY);
			_countdownText = null;
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

			// Checked together so two chests appearing at once get one combined bubble.
			bool normal = Check(_normal);
			bool emote = Check(_emote);
			if (normal || emote)
				Notify(normal ? _normal : null, emote ? _emote : null);
			UpdateCountdown();
		}

		private void UpdateCountdown()
		{
			bool show = _config.ChestEnabled && _config.ChestCountdown && (Shop.NormalShop != null || Shop.EmoteShop != null);
			if (!show)
			{
				if (_countdown.gameObject.activeSelf)
				{
					_countdown.gameObject.SetActive(false);
					OnDemandRenderHelper.Instance?.ResumeRenderingForDuration(0.05f);
				}
				_countdownText = null;
				return;
			}

			var sb = new StringBuilder();
			AppendStatus(sb, Shop.NormalShop, ModText.Get("Chest", "宝箱"));
			AppendStatus(sb, Shop.EmoteShop, ModText.Get("Emote", "エモート"));
			var text = sb.ToString().TrimStart('\n');
			if (text == _countdownText)
				return;

			_countdownText = text;
			_countdown.text = text;
			_countdown.gameObject.SetActive(true);
			// The game may have paused rendering in the background; show the new time.
			OnDemandRenderHelper.Instance?.ResumeRenderingForDuration(0.05f);
		}

		/// <summary>True when this chest should be announced now.</summary>
		private bool Check(Watched watched)
		{
			if (watched == null || watched.Shop == null || Pets.Instance == null)
				return false;

			bool buyable = watched.Shop.CanGetChest;
			bool justBecameBuyable = buyable && !watched.WasBuyable;
			watched.WasBuyable = buyable;

			if (!buyable || !_config.ChestEnabled)
				return false;

			float reminder = _config.ChestReminderMinutes * 60f;
			bool remind = reminder > 0f && Time.unscaledTime - watched.LastNotice >= reminder;
			if (!justBecameBuyable && !remind)
				return false;

			watched.LastNotice = Time.unscaledTime;
			return true;
		}

		/// <summary>True while a chest is out in the shop (the only time a notice can appear).</summary>
		public static bool AnyChestOut =>
			(Shop.NormalShop != null && Shop.NormalShop.ChestIsReady) || (Shop.EmoteShop != null && Shop.EmoteShop.ChestIsReady);

		/// <summary>Shows the notice once for a chest that is out, to see what it looks like.</summary>
		public void Preview()
		{
			var normal = Shop.NormalShop != null && Shop.NormalShop.ChestIsReady ? new Watched { Shop = Shop.NormalShop } : null;
			var emote = Shop.EmoteShop != null && Shop.EmoteShop.ChestIsReady ? new Watched { Shop = Shop.EmoteShop, IsEmote = true } : null;
			if (normal != null || emote != null)
				Notify(normal, emote, preview: true);
		}

		private void Notify(Watched normal, Watched emote, bool preview = false)
		{
			Log.InfoT(this, preview ? "Chest notice preview."
				: $"Can be bought: {(normal != null ? "chest " : "")}{(emote != null ? "emote chest" : "")}".TrimEnd());

			if (_config.ChestBubble)
			{
				_bubbleText.text = normal != null && emote != null
					? ModText.Get("Chest and emote chest ready!", "宝箱とエモート宝箱が買えるよ！")
					: emote != null
						? ModText.Get("Emote chest ready!", "エモート宝箱が買えるよ！")
						: ModText.Get("Chest ready!", "宝箱が買えるよ！");
				_bubbleTime = Time.unscaledTime;
				_bubble.gameObject.SetActive(true);
				_render.SetFullRate(true);
			}

			if (_config.ChestShake)
			{
				Shake(normal);
				Shake(emote);
			}
		}

		private static void Shake(Watched watched)
		{
			if (watched == null || watched.Shop == null)
				return;

			// The game's own shake tween on the shop button: purely visual.
			var shake = watched.Shop.GetComponentInChildren<ShakeAnimation>(true);
			if (shake != null)
			{
				shake.Shake();
				OnDemandRenderHelper.Instance?.ResumeRenderingForDuration(1.5f);
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

		private static void AppendStatus(StringBuilder sb, Shop shop, string name)
		{
			if (shop == null)
				return;

			sb.Append('\n').Append(name).Append(' ');
			if (!shop.ChestIsReady)
			{
				int seconds = Mathf.Max(0, shop.StockRefreshTimeLeft);
				sb.Append(seconds / 60).Append(':').Append((seconds % 60).ToString("00"));
				return;
			}

			if (shop.CanAffordChest)
			{
				sb.Append("<color=#ff8c26>").Append(ModText.Get("ready!", "買える！")).Append("</color>");
				return;
			}

			var item = shop.GetComponentInChildren<ShopItem>(true);
			int missing = item != null ? MissingPets(item.Price) : -1;
			sb.Append(missing > 0
				? ModText.Get($"{missing:#,0} to go", $"あと{missing:#,0}")
				: ModText.Get("out", "出てるよ"));
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
			if (_countdown != null)
				Destroy(_countdown.gameObject);
			if (_bubble != null)
				Destroy(_bubble.gameObject);
		}
	}
}
