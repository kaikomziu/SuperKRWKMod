using BongoCat;
using SuperKRWKMod.Config;
using SuperKRWKMod.Debug;
using SuperKRWKMod.ModList;
using SuperKRWKMod.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SuperKRWKMod.Features
{
	/// <summary>Something on screen whose position can be changed in the layout editor.</summary>
	public interface IMovableText
	{
		/// <summary>What gets moved (a label, or the box around it).</summary>
		RectTransform LayoutTarget { get; }

		/// <summary>Default position; the saved offset is relative to this.</summary>
		Vector2 LayoutBase { get; }

		/// <summary>ini section and keys holding the offset.</summary>
		string LayoutSection { get; }
		string LayoutKeyX { get; }
		string LayoutKeyY { get; }

		/// <summary>Show (with sample text if needed) while editing; restore afterwards.</summary>
		void SetEditing(bool editing);
	}

	/// <summary>
	/// Drag the mod's on-screen text to new places. Started from Settings &gt; Mods; each item gets an
	/// orange frame to grab, and "Done" saves the offsets to superkrwk.ini.
	/// </summary>
	public class LayoutEditor : MonoBehaviour
	{
		private static readonly Color FrameColor = new Color(1f, 0.55f, 0.15f, 0.35f);
		private static readonly Vector2 DonePosition = new Vector2(0f, 200f);

		private ModConfig _config;
		private Transform _ui;
		private TMP_Text _style;
		private Action _onChanged;
		private readonly List<IMovableText> _items = new List<IMovableText>();
		private readonly List<GameObject> _frames = new List<GameObject>();
		private readonly RenderDemand _render = new RenderDemand();
		private GameObject _done;

		public bool IsEditing { get; private set; }

		public void Init(ModConfig config, Transform ui, TMP_Text style, Action onChanged)
		{
			_config = config;
			_ui = ui;
			_style = style;
			_onChanged = onChanged;
		}

		public void Add(IMovableText item)
		{
			_items.Add(item);
		}

		public void Begin()
		{
			if (IsEditing)
				return;

			IsEditing = true;
			// Close the game menu so the text underneath can be reached.
			MainCat.Instance?.transform.Find("MenuNew")?.GetComponent<ToggleGameObject>()?.SetActive(false);

			foreach (var item in _items)
			{
				item.SetEditing(true);
				_frames.Add(AddFrame(item));
			}

			_done = CreateDoneButton();
			_render.SetFullRate(true);
			Log.InfoT(this, "Layout editing started.");
		}

		public void End()
		{
			if (!IsEditing)
				return;

			IsEditing = false;
			foreach (var frame in _frames)
				if (frame != null)
					Destroy(frame);
			_frames.Clear();
			if (_done != null)
				Destroy(_done);

			foreach (var item in _items)
				item.SetEditing(false);

			_render.SetFullRate(false);
			_onChanged?.Invoke();
			Log.InfoT(this, "Layout editing finished.");
		}

		/// <summary>Puts every item back at its default position.</summary>
		public void ResetAll()
		{
			foreach (var item in _items)
			{
				_config.Set(item.LayoutSection, item.LayoutKeyX, "0");
				_config.Set(item.LayoutSection, item.LayoutKeyY, "0");
			}
			_onChanged?.Invoke();
		}

		private void Update()
		{
			// Enter also finishes (Escape is the game's menu key).
			if (IsEditing && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)))
				End();
		}

		private GameObject AddFrame(IMovableText item)
		{
			var frame = new GameObject("SuperKRWKEditFrame");
			frame.layer = item.LayoutTarget.gameObject.layer;
			var rect = frame.AddComponent<RectTransform>();
			rect.SetParent(item.LayoutTarget, false);
			rect.anchorMin = Vector2.zero;
			rect.anchorMax = Vector2.one;
			// Stretched over the target plus a small margin (offsetMin/Max setters are stripped).
			rect.sizeDelta = new Vector2(8f, 6f);
			rect.anchoredPosition = Vector2.zero;
			// Not part of a box's layout.
			frame.AddComponent<LayoutElement>().ignoreLayout = true;

			// A visible, mouse-catching surface: this is what you grab.
			var image = frame.AddComponent<Image>();
			image.color = FrameColor;
			image.raycastTarget = true;

			var handle = frame.AddComponent<DragHandle>();
			handle.Target = item.LayoutTarget;
			handle.Released = () => Save(item);
			return frame;
		}

		private void Save(IMovableText item)
		{
			var offset = item.LayoutTarget.anchoredPosition - item.LayoutBase;
			_config.Set(item.LayoutSection, item.LayoutKeyX, Mathf.Round(offset.x).ToString(CultureInfo.InvariantCulture));
			_config.Set(item.LayoutSection, item.LayoutKeyY, Mathf.Round(offset.y).ToString(CultureInfo.InvariantCulture));
		}

		private GameObject CreateDoneButton()
		{
			var label = UiUtil.CreateBoxedLabel("SuperKRWKLayoutDone", _ui, _style, 10f, DonePosition, new Vector2(0.5f, 0.5f));
			label.text = ModText.Get("Drag the text, then click here (or Enter) when done",
				"文字をドラッグで移動 → ここをクリック（Enter）で完了");
			var box = UiUtil.BoxOf(label);
			var image = box.GetComponent<Image>();
			image.enabled = true; // always visible, even with boxes switched off
			image.raycastTarget = true;
			var button = box.gameObject.AddComponent<Button>();
			button.onClick.AddListener(End);
			return box.gameObject;
		}

		private void OnDisable()
		{
			End();
		}
	}

	/// <summary>Moves its target with the mouse; swallows the drag so the cat doesn't move too.</summary>
	public class DragHandle : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
	{
		public RectTransform Target;
		public Action Released;

		public void OnBeginDrag(PointerEventData eventData)
		{
		}

		public void OnDrag(PointerEventData eventData)
		{
			if (Target == null)
				return;

			// Screen pixels to the parent's local units (the UI is scaled with the cat).
			var parent = Target.parent;
			float scale = parent != null ? parent.lossyScale.x : 1f;
			if (Mathf.Abs(scale) < 0.0001f)
				scale = 1f;
			Target.anchoredPosition += eventData.delta / scale;
		}

		public void OnEndDrag(PointerEventData eventData)
		{
			Released?.Invoke();
		}
	}
}
