using SuperKRWKMod.Debug;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace SuperKRWKMod.ModList
{
	/// <summary>
	/// Adds a "Mods" category to the game's Settings view, listing every registered mod.
	/// The page is assembled from copies of harmless game widgets (header, text, divider, checkbox row)
	/// with all of the game's behaviour on them removed, so it matches the current UI theme.
	/// </summary>
	public class ModsSettingsPage : MonoBehaviour, IModPageBuilder
	{
		private const string ButtonName = "ModsSettings";
		private const string PageName = "Mods";

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

		public bool Init(MenuTabs menuTabs)
		{
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
				Log.ErrorT(this, "Settings UI layout not recognised; Mods page disabled.");
				return false;
			}

			_scroll = settingsView.GetComponentInChildren<ScrollRect>(true);

			CreateButton(otherButton, tabButtons);
			CreatePage(otherPage);
			HookGameTabs(tabButtons);
			Rebuild();
			ModRegistry.Changed += Rebuild;
			ModText.LanguageChanged += OnLanguageChanged;

			Log.InfoT(this, "Mods settings page added.");
			return true;
		}

		private void OnDestroy()
		{
			ModRegistry.Changed -= Rebuild;
			ModText.LanguageChanged -= OnLanguageChanged;
		}

		private void CreateButton(Transform template, Transform parent)
		{
			var go = Instantiate(template.gameObject, parent, false);
			go.name = ButtonName;
			go.transform.SetSiblingIndex(template.GetSiblingIndex() + 1);

			_button = go.GetComponent<Button>();
			_button.onClick = new Button.ButtonClickedEvent();
			_button.onClick.AddListener(Open);
			_button.interactable = true;

			var label = go.GetComponentInChildren<TMP_Text>(true);
			_buttonLabel = label;
			SetStaticText(label, ModText.Get("Mods", "MOD"));
		}

		private void CreatePage(Transform template)
		{
			_page = new GameObject(PageName);
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
			layout.childForceExpandHeight = templateLayout.childForceExpandHeight;

			_page.SetActive(false);
		}

		private void HookGameTabs(Transform tabButtons)
		{
			for (int i = 0; i < tabButtons.childCount; i++)
			{
				var child = tabButtons.GetChild(i);
				var button = child.GetComponent<Button>();
				if (button == null || button == _button)
					continue;

				_gameButtons.Add(button);
				var pageName = child.name.Replace("Settings", "");
				button.onClick.AddListener(() => OnGameTabClicked(button, pageName));
			}
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
			_button.interactable = false;

			// Rebuild so live details (records) are current.
			Rebuild();
			if (_scroll != null)
				_scroll.verticalNormalizedPosition = 1f;
		}

		private void OnLanguageChanged()
		{
			SetStaticText(_buttonLabel, ModText.Get("Mods", "MOD"));
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
			SetStaticText(header.Find("CategoryTxt")?.GetComponent<TMP_Text>(), ModText.Get("Mods", "MOD"));

			int count = ModRegistry.Mods.Count;
			AddText(ModText.Get($"{count} mod(s) installed", $"{count}個のMODが導入されています"), 0.8f);

			foreach (var mod in ModRegistry.Mods)
			{
				Clone(_dividerTemplate);
				AddText($"<b>{mod.Name}</b> <size=80%>v{mod.Version}</size>", 1.1f);
				AddOptionalText(mod.Author, 0.75f);
				AddOptionalText(mod.Description, 0.8f);

				foreach (var toggle in mod.Toggles)
					AddToggle(toggle);

				AddOptionalText(mod.Hotkeys, 0.75f);

				if (mod.BuildExtra != null)
				{
					try
					{
						mod.BuildExtra(this);
					}
					catch (System.Exception e)
					{
						Log.ExceptionT(this, e, $"{mod.Name} failed to build its controls");
					}
				}

				var details = mod.Details?.Invoke();
				if (!string.IsNullOrEmpty(details))
				{
					Clone(_dividerTemplate);
					AddText(details, 0.75f, TextAlignmentOptions.Left);
				}
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

		void IModPageBuilder.Divider()
		{
			Clone(_dividerTemplate);
		}

		void IModPageBuilder.Refresh()
		{
			Rebuild();
		}

		private void AddOptionalText(System.Func<string> text, float scale)
		{
			var value = text?.Invoke();
			if (!string.IsNullOrEmpty(value))
				AddText(value, scale);
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
