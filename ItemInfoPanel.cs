using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Valheim.UI;

namespace HeroRadialMenusMod
{
    /// <summary>
    /// The item stats window the vanilla radial shows beside the wheel: armour
    /// and carry-weight rows on top, then the hovered item's name and its
    /// inventory tooltip.
    ///
    /// Not rebuilt by hand — the vanilla InventoryInfo object is cloned out of
    /// Hud's radial menu, so fonts, backgrounds and icons match the game 1:1.
    /// Its own RadialInventoryInfo component stays on for the armour/weight
    /// rows (RefreshInfo / RefreshWeight are public); the tooltip part, which
    /// vanilla drives through its internal element types and tween manager,
    /// is driven from here.
    /// </summary>
    internal sealed class ItemInfoPanel
    {
        private static readonly AccessTools.FieldRef<RadialInventoryInfo, RectTransform> TooltipRef =
            AccessTools.FieldRefAccess<RadialInventoryInfo, RectTransform>("m_itemTooltip");
        private static readonly AccessTools.FieldRef<RadialInventoryInfo, TextMeshProUGUI> TitleRef =
            AccessTools.FieldRefAccess<RadialInventoryInfo, TextMeshProUGUI>("m_itemTitleText");
        private static readonly AccessTools.FieldRef<RadialInventoryInfo, TextMeshProUGUI> BodyRef =
            AccessTools.FieldRefAccess<RadialInventoryInfo, TextMeshProUGUI>("m_itemTooltipText");
        private static readonly AccessTools.FieldRef<RadialInventoryInfo, float> MinHeightRef =
            AccessTools.FieldRefAccess<RadialInventoryInfo, float>("m_toolTipMinHeight");
        private static readonly AccessTools.FieldRef<RadialInventoryInfo, float> MaxHeightRef =
            AccessTools.FieldRefAccess<RadialInventoryInfo, float>("m_toolTipMaxHeight");

        // Vanilla prefab width; the height is laid out by the panel itself.
        private const float PanelWidth  = 450f;
        private const float PanelHeight = 760f;
        // Gap between the wheel's outer edge and the panel.
        private const float EdgeGap = 40f;
        // Tooltip grow/shrink rate, roughly the vanilla tween's feel.
        private const float ResizeSharpness = 14f;
        private const float WeightRefreshSeconds = 0.25f;

        private GameObject? _root;
        private RectTransform? _rootRect;
        private RadialInventoryInfo? _info;
        private RectTransform? _tooltip;
        private TextMeshProUGUI? _title;
        private TextMeshProUGUI? _body;
        private float _minHeight = 70f;
        private float _maxHeight = 630f;

        private ItemDrop.ItemData? _item;
        private float _height;
        private float _targetHeight;
        private float _nextWeightRefresh;
        private bool _createFailed;

        public GameObject? Root => _root;

        /// <summary>
        /// Clone the vanilla panel under <paramref name="parent"/>. Safe to call
        /// repeatedly; it retries until the vanilla radial exists, and gives up
        /// for good (with one warning) if the game no longer has the panel.
        /// </summary>
        public bool EnsureCreated(Transform parent)
        {
            if (_root != null) return true;
            if (_createFailed) return false;

            var source = FindVanillaPanel();
            if (source == null) return false;

            try
            {
                _root = Object.Instantiate(source.gameObject, parent, false);
                _root.name = "ItemInfo";
                _info = _root.GetComponent<RadialInventoryInfo>();
                _tooltip = TooltipRef(_info);
                _title = TitleRef(_info);
                _body = BodyRef(_info);
                _minHeight = MinHeightRef(_info);
                _maxHeight = MaxHeightRef(_info);

                // The vanilla radial may sit on its own canvas; ours is the HUD,
                // so anything that would make the clone a separate canvas goes.
                var canvas = _root.GetComponent<Canvas>();
                if (canvas != null) Object.Destroy(canvas);
                foreach (var g in _root.GetComponentsInChildren<Graphic>(true))
                    g.raycastTarget = false;

                _rootRect = (RectTransform)_root.transform;
                _rootRect.anchorMin = _rootRect.anchorMax = new Vector2(0.5f, 0.5f);
                _rootRect.pivot = new Vector2(1f, 0.5f);
                _rootRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);
                _rootRect.localRotation = Quaternion.identity;

                SetHeight(0f);
                _root.SetActive(false);

                Plugin.LogDebug($"ItemInfoPanel: клоновано ванільну панель '{source.name}', " +
                    $"масштаб джерела {source.transform.lossyScale.x:0.###}, колеса {parent.lossyScale.x:0.###}.");
                return true;
            }
            catch (System.Exception ex)
            {
                if (_root != null) Object.Destroy(_root);
                _root = null;
                _createFailed = true;
                Plugin.Log.LogWarning($"ItemInfoPanel: не вдалося створити вікно статів предмета: {ex.Message}");
                return false;
            }
        }

        private static RadialInventoryInfo? FindVanillaPanel()
        {
            var hud = Hud.instance;
            if (hud != null && hud.m_radialMenu != null)
            {
                var own = hud.m_radialMenu.GetComponentInChildren<RadialInventoryInfo>(true);
                if (own != null) return own;
            }
            // Fallback: any loaded instance or the prefab itself.
            foreach (var info in Resources.FindObjectsOfTypeAll<RadialInventoryInfo>())
                if (info != null) return info;
            return null;
        }

        /// <summary>Place the panel left of a wheel whose sectors end at <paramref name="outerRadius"/>.</summary>
        public void Layout(float outerRadius)
        {
            if (_rootRect == null) return;
            float scale = Mathf.Clamp(Plugin.ItemInfoScale.Value, 0.3f, 3f);
            _rootRect.localScale = Vector3.one * scale;
            _rootRect.anchoredPosition = new Vector2(
                -(outerRadius + EdgeGap) + Plugin.ItemInfoOffsetX.Value,
                Plugin.ItemInfoOffsetY.Value);
        }

        public void Show()
        {
            if (_root == null || !Plugin.ShowItemInfo.Value) return;
            _item = null;
            _targetHeight = 0f;
            SetHeight(0f);
            if (_title != null) _title.text = "";
            _root.SetActive(true);
            _info?.RefreshInfo();
            _nextWeightRefresh = Time.unscaledTime + WeightRefreshSeconds;
        }

        public void Hide()
        {
            _item = null;
            if (_root != null) _root.SetActive(false);
        }

        /// <summary>Point the tooltip at the hovered item, or null to fold it away.</summary>
        public void SetItem(ItemDrop.ItemData? item)
        {
            if (_root == null || !_root.activeSelf) return;
            if (ReferenceEquals(item, _item)) return;
            _item = item;

            if (item?.m_shared == null)
            {
                _targetHeight = 0f;
                return;
            }

            var loc = Localization.instance;
            string name = item.m_shared.m_name;
            string tooltip = item.GetTooltip();
            if (_title != null) _title.text = loc != null ? loc.Localize(name) : name;
            if (_body != null) _body.text = loc != null ? loc.Localize(tooltip) : tooltip;

            // The tooltip is inactive while folded, and an inactive body has no
            // width to measure against — open it first, then lay it out.
            if (_height < _minHeight) SetHeight(_minHeight);
            if (_rootRect != null) LayoutRebuilder.ForceRebuildLayoutImmediate(_rootRect);

            // Same sizing rule as vanilla StartResize.
            float wanted = _minHeight;
            if (_body != null && _body.gameObject.activeInHierarchy)
            {
                float width = _body.rectTransform.rect.width;
                if (width <= 1f) width = PanelWidth - 20f;
                wanted = _body.GetPreferredValues(_body.text, width, 0f).y + _minHeight + 10f;
            }
            _targetHeight = Mathf.Clamp(wanted, _minHeight, _maxHeight);

            _info?.RefreshInfo();
        }

        public void Tick()
        {
            if (_root == null || !_root.activeSelf) return;

            if (!Mathf.Approximately(_height, _targetHeight))
            {
                float t = 1f - Mathf.Exp(-ResizeSharpness * Time.unscaledDeltaTime);
                float h = Mathf.Lerp(_height, _targetHeight, t);
                if (Mathf.Abs(h - _targetHeight) < 0.5f) h = _targetHeight;

                // Vanilla MinHeightCheck: once a closing tooltip drops below the
                // minimum it snaps shut and the stale title is cleared.
                if (_targetHeight <= 0f && h < _minHeight)
                {
                    h = 0f;
                    if (_title != null) _title.text = "";
                }
                SetHeight(h);
            }

            // Weight changes as items are used from the wheel.
            if (Time.unscaledTime >= _nextWeightRefresh)
            {
                _nextWeightRefresh = Time.unscaledTime + WeightRefreshSeconds;
                _info?.RefreshWeight();
            }
        }

        private void SetHeight(float h)
        {
            _height = h;
            if (_tooltip == null) return;
            _tooltip.sizeDelta = new Vector2(_tooltip.sizeDelta.x, h);
            _tooltip.gameObject.SetActive(h > 0f);
            if (_rootRect != null) LayoutRebuilder.MarkLayoutForRebuild(_rootRect);
        }
    }
}
