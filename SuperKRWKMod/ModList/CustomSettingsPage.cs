using SuperKRWKMod.Debug;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace SuperKRWKMod.ModList
{
	/// <summary>
	/// Adds a category of our own to the game's Settings view (e.g. "Mods", "Records").
	/// The page is assembled from copies of harmless game widgets (header, text, divider, checkbox row,
	/// button) with all of the game's behaviour on them removed, so it matches the current UI theme.
	/// </summary>
	public class CustomSettingsPage : MonoBehaviour, IModPageBuilder
	{
		private static readonly List<CustomSettingsPage> All = new List<CustomSettingsPage>();

		private string _id;
		private System.Func<string> _title;
		private System.Action<IModPageBuilder> _build;

		private struct ToggleRow
		{
			public ModToggle Toggle;
			public GameObject Check;
		}

		private readonly List<ToggleRow> _rows = new List<ToggleRow>();
		private readonly List<Button> _gameButtons = new List<Button>();
		private Transform _content;
		private GameObject _page;
		private Button _button;
		private TMP_Text _buttonLabel;
		private ScrollRect _scroll;

		// Templates (never modified).
		private Transform _headerTemplate;
		private Transform _textTemplate;
		private Transform _dividerTemplate;
		private Transform _toggleTemplate;
		private Transform _buttonTemplate;

		/// <param name="id">Unique name for the button/page objects.</param>
		/// <param name="title">Category name shown on the button and in the page header.</param>
		/// <param name="build">Fills the page; called every time it is opened or refreshed.</param>
		public bool Init(MenuTabs menuTabs, string id, System.Func<string> title, System.Action<IModPageBuilder> build)
		{
			_id = id;
			_title = title;
			_build = build;

			var settingsView = menuTabs.transform.Find("Content/SettingsView");
			var tabButtons = settingsView?.Find("TabButtons");
			var otherButton = tabButtons?.Find("OtherSettings");
			_content = settingsView?.Find("ScrollView/Viewport/Content");
			var otherPage = _content?.Find("Other");
			var generalPage = _content?.Find("General");

			_headerTemplate = otherPage?.Find("HeaderTxt");
			_textTemplate = otherPage?.Find("LifetimeTaps");
			_dividerTemplate = otherPage?.Find("Divider");
			_toggleTemplate = generalPage?.Find("Autostart");
			_buttonTemplate = otherPage?.Find("PlayTutBtn");

			if (otherButton == null || otherPage == null || _headerTemplate == null || _textTemplate == null || _dividerTemplate == null || _toggleTemplate == null || _buttonTemplate == null)
			{
				Log.ErrorT(this, $"Settings UI layout not recognised; {id} page disabled.");
				return false;
			}

			_scroll = settingsView.GetComponentInChildren<ScrollRect>(true);

			CreateButton(otherButton, tabButtons);
			CreatePage(otherPage);
			HookGameTabs(tabButtons);
			All.Add(this);
			Rebuild();
			ModRegistry.Changed += Rebuild;
			ModText.LanguageChanged += OnLanguageChanged;

			Log.InfoT(this, $"{id} settings page added.");
			return true;
		}

		private void OnDestroy()
		{
			ModRegistry.Changed -= Rebuild;
			ModText.LanguageChanged -= OnLanguageChanged;
			All.Remove(this);
		}

		private void CreateButton(Transform template, Transform parent)
		{
			var go = Instantiate(template.gameObject, parent, false);
			go.name = _id + "Settings";
			// After "Other" and any of our own categories added before this one.
			go.transform.SetSiblingIndex(template.GetSiblingIndex() + 1 + All.Count);

			_button = go.GetComponent<Button>();
			_button.onClick = new Button.ButtonClickedEvent();
			_button.onClick.AddListener(Open);
			_button.interactable = true;

			var label = go.GetComponentInChildren<TMP_Text>(true);
			_buttonLabel = label;
			SetStaticText(label, _title());
		}

		private void CreatePage(Transform template)
		{
			_page = new GameObject(_id);
			_page.layer = template.gameObject.layer;
			var rect = _page.AddComponent<RectTransform>();
			rect.SetParent(_content, false);

			var templateRect = (RectTransform)template;
			rect.anchorMin = templateRect.anchorMin;
			rect.anchorMax = templateRect.anchorMax;
			rect.pivot = templateRect.pivot;
			rect.sizeDelta = templateRect.sizeDelta;

			var templateLayout = template.GetComponent<VerticalLayoutGroup>();
			var layout = _page.AddComponent<VerticalLayoutGroup>();
			layout.padding = templateLayout.padding;
			layout.spacing = templateLayout.spacing;
			layout.childAlignment = templateLayout.childAlignment;
			layout.childControlWidth = templateLayout.childControlWidth;
			layout.childControlHeight = templateLayout.childControlHeight;
			layout.childForceExpandWidth = templateLayout.childForceExpandWidth;
			// Rows keep their own height; the page grows or shrinks to fit them instead of spreading
			// short content over the template's fixed height.
			layout.childForceExpandHeight = false;
			var fitter = _page.AddComponent<ContentSizeFitter>();
			fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

			_page.SetActive(false);
		}

		private void HookGameTabs(Transform tabButtons)
		{
			for (int i = 0; i < tabButtons.childCount; i++)
			{
				var child = tabButtons.GetChild(i);
				var button = child.GetComponent<Button>();
				if (button == null || button == _button || IsCustomButton(button))
					continue;

				_gameButtons.Add(button);
				var pageName = child.name.Replace("Settings", "");
				button.onClick.AddListener(() => OnGameTabClicked(button, pageName));
			}
		}

		private static bool IsCustomButton(Button button)
		{
			foreach (var page in All)
				if (page._button == button)
					return true;
			return false;
		}

		private void OnGameTabClicked(Button button, string pageName)
		{
			if (!_page.activeSelf)
				return;

			// The game's Tabs already switched (or kept) its own page; make sure it is the one showing.
			_page.SetActive(false);
			_button.interactable = true;
			var page = _content.Find(pageName);
			if (page != null)
				page.gameObject.SetActive(true);
			button.interactable = false;
		}

		private void Open()
		{
			for (int i = 0; i < _content.childCount; i++)
				_content.GetChild(i).gameObject.SetActive(false);

			_page.SetActive(true);
			foreach (var button in _gameButtons)
				button.interactable = true;
			foreach (var page in All)
				page._button.interactable = page != this;

			// Rebuild so live content (records, app status) is current.
			Rebuild();
			if (_scroll != null)
				_scroll.verticalNormalizedPosition = 1f;
		}

		private void OnLanguageChanged()
		{
			SetStaticText(_buttonLabel, _title());
			Rebuild();
		}

		private void Rebuild()
		{
			if (_page == null)
				return;

			for (int i = _page.transform.childCount - 1; i >= 0; i--)
				Destroy(_page.transform.GetChild(i).gameObject);
			_rows.Clear();

			var header = Clone(_headerTemplate);
			// Keep "Settings" localized; replace the category name.
			SetStaticText(header.Find("CategoryTxt")?.GetComponent<TMP_Text>(), _title());

			try
			{
				_build(this);
			}
			catch (System.Exception e)
			{
				Log.ExceptionT(this, e, $"Failed to build the {_id} page");
			}

			RefreshToggles();
		}

		// --- IModPageBuilder --------------------------------------------------------------------

		void IModPageBuilder.Text(string text, float scale, bool leftAligned)
		{
			AddText(text, scale, leftAligned ? TextAlignmentOptions.Left : (TextAlignmentOptions?)null);
		}

		void IModPageBuilder.Toggle(ModToggle toggle)
		{
			AddToggle(toggle);
			RefreshToggles();
		}

		void IModPageBuilder.Button(string label, System.Action onClick)
		{
			var row = Clone(_buttonTemplate);
			var button = row.GetComponent<Button>();
			// Replace the template's own click handler (it starts the tutorial).
			button.onClick = new Button.ButtonClickedEvent();
			button.onClick.AddListener(() => onClick());
			button.interactable = true;
			DestroyChild(row, "HoverTooltip");
			SetStaticText(row.GetComponentInChildren<TMP_Text>(true), label);
		}

		void IModPageBuilder.BarChart(IList<BarItem> items)
		{
			const float labelWidth = 34f, valueWidth = 70f, maxBar = 100f, rowHeight = 14f;
			float max = 0f;
			foreach (var item in items)
				max = Mathf.Max(max, item.Value);

			foreach (var item in items)
			{
				var row = NewRow("SuperKRWKBarRow", rowHeight);

				var label = SmallText(row, item.Label, labelWidth, TextAlignmentOptions.Left);
				var bar = new GameObject("Bar");
				bar.layer = row.gameObject.layer;
				var barRect = bar.AddComponent<RectTransform>();
				barRect.SetParent(row, false);
				var image = bar.AddComponent<Image>();
				image.raycastTarget = false;
				var color = label.color;
				color.a = 0.75f;
				image.color = color;
				var barLayout = bar.AddComponent<LayoutElement>();
				// A sliver even for 0 so the row still reads as a bar chart.
				barLayout.preferredWidth = max > 0f ? Mathf.Max(2f, maxBar * item.Value / max) : 2f;
				barLayout.preferredHeight = rowHeight * 0.6f;

				SmallText(row, item.ValueText, valueWidth, TextAlignmentOptions.Left);
			}
		}

		void IModPageBuilder.Stepper(System.Func<string> label, System.Action decrease, System.Action increase)
		{
			var row = NewRow("SuperKRWKStepper", 24f);
			TMP_Text text = null;
			StepButton(row, "−", () => { decrease(); text.text = label(); });
			text = SmallText(row, label(), -1f, TextAlignmentOptions.Center);
			StepButton(row, "+", () => { increase(); text.text = label(); });
		}

		private RectTransform NewRow(string name, float height)
		{
			var go = new GameObject(name);
			go.layer = _page.layer;
			var rect = go.AddComponent<RectTransform>();
			rect.SetParent(_page.transform, false);
			// The page's layout may not control child sizes, so give the row its size explicitly
			// (a new RectTransform defaults to 100x100).
			rect.sizeDelta = new Vector2(((RectTransform)_textTemplate).rect.width, height);
			var layout = go.AddComponent<HorizontalLayoutGroup>();
			layout.spacing = 4f;
			layout.childAlignment = TextAnchor.MiddleLeft;
			layout.childControlWidth = true;
			layout.childControlHeight = true;
			layout.childForceExpandWidth = false;
			layout.childForceExpandHeight = false;
			var element = go.AddComponent<LayoutElement>();
			element.preferredHeight = height;
			element.minHeight = height;
			return rect;
		}

		/// <param name="width">Fixed width, or negative to take the remaining space.</param>
		private TMP_Text SmallText(Transform parent, string text, float width, TextAlignmentOptions alignment)
		{
			var go = Instantiate(_textTemplate.gameObject, parent, false);
			go.SetActive(true);
			var label = go.GetComponent<TMP_Text>();
			label.fontSize *= 0.75f;
			label.alignment = alignment;
			label.textWrappingMode = TextWrappingModes.NoWrap;
			SetStaticText(label, text);
			var element = go.GetComponent<LayoutElement>();
			if (element == null)
				element = go.AddComponent<LayoutElement>();
			if (width >= 0f)
			{
				element.preferredWidth = width;
				element.flexibleWidth = 0f;
			}
			else
			{
				element.flexibleWidth = 1f;
			}
			return label;
		}

		private void StepButton(Transform parent, string symbol, System.Action onClick)
		{
			var go = Instantiate(_buttonTemplate.gameObject, parent, false);
			go.SetActive(true);
			var button = go.GetComponent<Button>();
			// Replace the template's own click handler (it starts the tutorial).
			button.onClick = new Button.ButtonClickedEvent();
			button.onClick.AddListener(() => onClick());
			button.interactable = true;
			DestroyChild(go.transform, "HoverTooltip");
			SetStaticText(go.GetComponentInChildren<TMP_Text>(true), symbol);
			var element = go.GetComponent<LayoutElement>();
			if (element == null)
				element = go.AddComponent<LayoutElement>();
			element.preferredWidth = 28f;
			element.minWidth = 28f;
			element.flexibleWidth = 0f;
			element.preferredHeight = 22f;
		}

		void IModPageBuilder.Divider()
		{
			Clone(_dividerTemplate);
		}

		void IModPageBuilder.Refresh()
		{
			Rebuild();
		}

		private void AddText(string text, float scale)
		{
			AddText(text, scale, null);
		}

		private void AddText(string text, float scale, TextAlignmentOptions? alignment)
		{
			var label = Clone(_textTemplate).GetComponent<TMP_Text>();
			if (alignment.HasValue)
				label.alignment = alignment.Value;
			label.fontSize *= scale;
			label.textWrappingMode = TextWrappingModes.Normal;
			SetStaticText(label, text);

			// Let wrapped text grow instead of using the template's fixed height.
			var fitter = label.gameObject.GetComponent<ContentSizeFitter>();
			if (fitter == null)
				fitter = label.gameObject.AddComponent<ContentSizeFitter>();
			fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
		}

		private void AddToggle(ModToggle toggle)
		{
			var row = Clone(_toggleTemplate);

			// Drop the template's behaviour: its click handler, tooltip and availability overlay.
			var button = row.GetComponent<Button>();
			button.onClick = new Button.ButtonClickedEvent();
			button.onClick.AddListener(() =>
			{
				toggle.Set(!toggle.Get());
				RefreshToggles();
			});
			DestroyChild(row, "HoverTooltip");
			DestroyChild(row, "DisableOverlay");
			foreach (var behaviour in row.GetComponents<MonoBehaviour>())
			{
				var typeName = behaviour.GetType().Name;
				if (typeName == "HoverTooltip")
					Destroy(behaviour);
			}

			SetStaticText(row.Find("Content/Label")?.GetComponent<TMP_Text>(), toggle.Label?.Invoke());

			var box = row.Find("Content/Toggle");
			var check = box != null && box.childCount > 0 ? box.GetChild(0).gameObject : null;
			if (_rows.Count == 0)
				Log.InfoT(this, $"Checkbox mark object: {(check != null ? check.name : "none")}");
			_rows.Add(new ToggleRow { Toggle = toggle, Check = check });
		}

		private void RefreshToggles()
		{
			foreach (var row in _rows)
			{
				if (row.Check != null)
					row.Check.SetActive(row.Toggle.Get());
			}
		}

		private Transform Clone(Transform template)
		{
			var go = Instantiate(template.gameObject, _page.transform, false);
			go.name = template.name;
			go.SetActive(true);
			return go.transform;
		}

		private static void DestroyChild(Transform parent, string name)
		{
			var child = parent.Find(name);
			if (child != null)
				Destroy(child.gameObject);
		}

		/// <summary>Sets text that the game's localization must not overwrite.</summary>
		private static void SetStaticText(TMP_Text label, string text)
		{
			if (label == null)
				return;

			var localize = label.GetComponent<LocalizeStringEvent>();
			if (localize != null)
			{
				localize.enabled = false;
				Destroy(localize);
			}

			label.text = text;
		}
	}
}
