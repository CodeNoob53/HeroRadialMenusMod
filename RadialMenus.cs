using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Valheim.UI;

namespace HeroRadialMenusMod
{
    // =============================================
    // Отримуємо оригінальні текстури/спрайти орнаменту-плетінки
    // (T_radialHighlighter) та шеврона-вказівника (T_radialIndicator)
    // безпосередньо з ресурсів гри без сторонніх файлів чи base64.
    // =============================================
    public static class VanillaRadialSprites
    {
        private static Sprite? _highlighter;
        private static Sprite? _indicator;
        private static Sprite? _radialGlow;
        private static bool _loaded;

        public static Sprite? Highlighter
        {
            get
            {
                EnsureLoaded();
                return _highlighter;
            }
        }

        public static Sprite? Indicator
        {
            get
            {
                EnsureLoaded();
                return _indicator;
            }
        }

        public static Sprite RadialGlow
        {
            get
            {
                if (_radialGlow == null)
                    _radialGlow = CreateRadialGlowSprite();
                return _radialGlow;
            }
        }

        private static Sprite? _whiteGlow;
        private static readonly Dictionary<string, Sprite> _splitGlows = new Dictionary<string, Sprite>();

        /// <summary>Soft white glow, tinted per use (single effect colour).</summary>
        public static Sprite WhiteGlow => _whiteGlow ??= CreateGlowSprite(Color.white, 1f);

        /// <summary>
        /// Soft glow split left to right between the given colours, for items
        /// with balanced effects. Baked per colour combination and cached —
        /// there are only a handful.
        /// </summary>
        public static Sprite SplitGlow(Color[] colors, int count)
        {
            var key = new System.Text.StringBuilder();
            for (int i = 0; i < count; i++) key.Append(ColorUtility.ToHtmlStringRGB(colors[i]));
            string k = key.ToString();
            if (!_splitGlows.TryGetValue(k, out var sprite))
            {
                var stops = new Color[count];
                System.Array.Copy(colors, stops, count);
                sprite = CreateSplitGlowSprite(stops);
                _splitGlows[k] = sprite;
            }
            return sprite;
        }

        public static void EnsureLoaded()
        {
            if (_loaded && _highlighter != null && _indicator != null) return;

            try
            {
                if (Hud.instance != null && Hud.instance.m_radialMenu != null)
                {
                    var radial = Hud.instance.m_radialMenu;
                    var hlTr = AccessTools.FieldRefAccess<RadialBase, RectTransform>(radial, "m_highlighter");
                    if (hlTr != null && _highlighter == null)
                    {
                        var img = hlTr.GetComponentInChildren<Image>(true);
                        if (img != null && img.sprite != null)
                            _highlighter = img.sprite;
                    }

                    var curTr = AccessTools.FieldRefAccess<RadialBase, RectTransform>(radial, "m_cursor");
                    if (curTr != null && _indicator == null)
                    {
                        var img = curTr.GetComponentInChildren<Image>(true);
                        if (img != null && img.sprite != null)
                            _indicator = CreateWhiteSprite(img.sprite);
                    }
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogWarning($"VanillaRadialSprites: radialMenu lookup failed: {ex.Message}");
            }

            if (_highlighter == null || _indicator == null)
            {
                foreach (var sp in Resources.FindObjectsOfTypeAll<Sprite>())
                {
                    if (sp == null) continue;
                    string n = sp.name;
                    if (_highlighter == null && (n.IndexOf("radialHighlighter", System.StringComparison.OrdinalIgnoreCase) >= 0 || n.IndexOf("T_radialHighlighter", System.StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        _highlighter = sp;
                    }
                    if (_indicator == null && (n.IndexOf("radialIndicator", System.StringComparison.OrdinalIgnoreCase) >= 0 || n.IndexOf("T_radialIndicator", System.StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        _indicator = CreateWhiteSprite(sp);
                    }
                    if (_highlighter != null && _indicator != null) break;
                }
            }

            _loaded = true;
            Plugin.LogDebug(
                $"VanillaRadialSprites: loaded Highlighter={(_highlighter != null ? _highlighter.name : "null")}, " +
                $"Indicator={(_indicator != null ? _indicator.name : "null")}");
        }

        // Створює білу маску спрайта, зберігаючи оригінальний альфа-канал,
        // щоб при накладанні кольору він 1:1 збігався з рамкою та орнаментом
        private static Sprite CreateWhiteSprite(Sprite source)
        {
            if (source == null) return null!;
            try
            {
                var srcTex = source.texture;
                if (srcTex == null) return source;

                int w = Mathf.RoundToInt(source.rect.width);
                int h = Mathf.RoundToInt(source.rect.height);
                int x = Mathf.RoundToInt(source.rect.x);
                int y = Mathf.RoundToInt(source.rect.y);

                var rt = RenderTexture.GetTemporary(srcTex.width, srcTex.height, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(srcTex, rt);

                var prevActive = RenderTexture.active;
                RenderTexture.active = rt;

                var readable = new Texture2D(w, h, TextureFormat.RGBA32, false);
                readable.ReadPixels(new Rect(x, y, w, h), 0, 0);
                readable.Apply();

                RenderTexture.active = prevActive;
                RenderTexture.ReleaseTemporary(rt);

                var pixels = readable.GetPixels32();
                for (int i = 0; i < pixels.Length; i++)
                {
                    pixels[i] = new Color32(255, 255, 255, pixels[i].a);
                }
                readable.SetPixels32(pixels);
                readable.Apply();

                var whiteSprite = Sprite.Create(
                    readable,
                    new Rect(0f, 0f, w, h),
                    new Vector2(0.5f, 0.5f),
                    source.pixelsPerUnit,
                    0,
                    SpriteMeshType.FullRect,
                    source.border
                );
                whiteSprite.name = source.name + "_White";
                return whiteSprite;
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogWarning($"VanillaRadialSprites.CreateWhiteSprite failed: {ex.Message}");
                return source;
            }
        }

        // Створює круглу м'яку підкладку світло-голубого кольору з радіальним градієнтом
        // (у центрі світло-голубий, з краю прозорий) як в оригіналі для активного предмета
        private static Sprite CreateRadialGlowSprite() =>
            CreateGlowSprite(new Color(0.40f, 0.75f, 1.0f), 0.85f);

        // Кругла м'яка пляма заданого кольору: у центрі непрозора, до краю зникає.
        private static Sprite CreateGlowSprite(Color color, float peakAlpha)
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            float center = (size - 1) * 0.5f;
            float maxR = center;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center) / maxR;
                    float dy = (y - center) / maxR;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist >= 1f)
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                    else
                    {
                        float falloff = 1f - dist;
                        float alpha = falloff * falloff;
                        pixels[y * size + x] = new Color(color.r, color.g, color.b, alpha * peakAlpha);
                    }
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        }

        // М'яка пляма, як CreateGlowSprite, але колір переходить зліва направо між
        // кількома кольорами. Перехід стиснутий до середини (smoothstep), тож
        // половини читаються як два окремі кольори, а не одна змішана пляма.
        private static Sprite CreateSplitGlowSprite(Color[] stops)
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            var pixels = new Color[size * size];
            float center = (size - 1) * 0.5f;
            int segments = stops.Length - 1;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center) / center;
                    float dy = (y - center) / center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist >= 1f) { pixels[y * size + x] = Color.clear; continue; }

                    float u = (float)x / (size - 1) * segments;
                    int seg = Mathf.Min((int)u, segments - 1);
                    float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((u - seg - 0.3f) / 0.4f));
                    var c = Color.Lerp(stops[seg], stops[seg + 1], t);
                    float falloff = 1f - dist;
                    pixels[y * size + x] = new Color(c.r, c.g, c.b, falloff * falloff);
                }
            }
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        }
    }

    // =============================================
    // ПРОЦЕДУРНА ГРАФІКА РАДІАЛЬНОГО МЕНЮ
    // Підтримує від 3 секторів, паралельні зазори 14px, плавні анімації
    // зміни ховеру для кожного сектора окремо та золоту рамку.
    // =============================================
    public sealed class ProceduralRadialGraphic : MaskableGraphic
    {
        private int _segments = 8;
        private int _itemCount = 0;
        private float[] _sectorWeights = new float[16];
        private float _distance = 320f;
        private Color _unselectedColor = new Color(0.03f, 0.03f, 0.03f, 0.92f);
        private Color _emptyColor = new Color(0.02f, 0.02f, 0.02f, 0.65f);
        private Color _hoverColor = new Color(0.25f, 0.15f, 0.03f, 0.90f);
        private Color _frameColor = new Color(0.784f, 0.588f, 0.157f, 0.95f);
        private float _frameWidth = 4f;
        private int   _flashIndex = -1;
        private float _flashAmount;
        private Color _flashColor;

        public void UpdateState(int segments, int itemCount, float[] sectorWeights,
            float distance, Color unselectedColor, Color emptyColor, Color hoverColor,
            Color frameColor, float frameWidth,
            int flashIndex = -1, float flashAmount = 0f, Color flashColor = default)
        {
            _flashIndex = flashIndex;
            _flashAmount = Mathf.Clamp01(flashAmount);
            _flashColor = flashColor;
            _segments = Mathf.Max(3, segments);
            _itemCount = itemCount;
            _distance = distance;
            _unselectedColor = unselectedColor;
            _emptyColor = emptyColor;
            _hoverColor = hoverColor;
            _frameColor = frameColor;
            _frameWidth = Mathf.Max(1f, frameWidth);

            if (_sectorWeights.Length < _segments)
                _sectorWeights = new float[_segments];

            for (int i = 0; i < _segments; i++)
            {
                _sectorWeights[i] = i < sectorWeights.Length ? sectorWeights[i] : 0f;
            }

            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_segments < 3) return;

            float innerRadius = Mathf.Max(10f, _distance - 86f);
            float outerRadius = _distance + 94f;
            float centerDiscRadius = Mathf.Max(10f, innerRadius - 35f);

            // 1. Центральний диск (менш прозорий, 92% непрозорості)
            AddCircleFilled(vh, Vector2.zero, centerDiscRadius, new Color(0.03f, 0.03f, 0.03f, 0.92f), 36);
            AddArcRibbon(vh, Vector2.zero, centerDiscRadius - 1.5f, centerDiscRadius + 1.5f, 0f, 360f, new Color(0.35f, 0.28f, 0.14f, 0.6f), 36);

            float step = 360f / _segments;
            float halfStep = step * 0.5f;

            // Зазор між секторами: стала ширина 28px у просторі екрана (по 14px з кожного боку лінії поділу)
            const float gapHalfPx = 14f;
            float halfGapInner = (gapHalfPx / innerRadius) * Mathf.Rad2Deg;
            float halfGapOuter = (gapHalfPx / outerRadius) * Mathf.Rad2Deg;

            const float nudgeDist = 20f;

            // 2. Сектори з плавною індивідуальною анімацією ховеру
            for (int i = 0; i < _segments; i++)
            {
                float w = (i < _sectorWeights.Length) ? _sectorWeights[i] : 0f;
                float midAngle = UIMath.Mod(i * step, 360f);
                var dir = ClockDir(midAngle);
                var offset = dir * (nudgeDist * w);

                float startOut = midAngle - (halfStep - halfGapOuter);
                float endOut   = midAngle + (halfStep - halfGapOuter);
                float startIn  = midAngle - (halfStep - halfGapInner);
                float endIn    = midAngle + (halfStep - halfGapInner);

                Color baseCol = (i < _itemCount) ? _unselectedColor : _emptyColor;
                Color fillColor = Color.Lerp(baseCol, _hoverColor, w);
                if (i == _flashIndex && _flashAmount > 0f)
                    fillColor = Color.Lerp(fillColor, _flashColor, _flashAmount);

                AddAnnularSectorFilled(vh, offset, innerRadius, outerRadius, startIn, endIn, startOut, endOut, fillColor);

                // Золота контурна рамка плавно з'являється разом із вагою сектора
                if (Plugin.RadialShowFrame.Value && w > 0.01f)
                {
                    Color fCol = new Color(_frameColor.r, _frameColor.g, _frameColor.b, _frameColor.a * w);
                    float halfW = _frameWidth * 0.5f;
                    float deltaOut = (halfW / outerRadius) * Mathf.Rad2Deg;
                    float deltaIn  = (halfW / innerRadius)  * Mathf.Rad2Deg;

                    AddArcRibbon(vh, offset, outerRadius - halfW, outerRadius + halfW,
                        startOut - deltaOut, endOut + deltaOut, fCol);

                    AddArcRibbon(vh, offset, innerRadius - halfW, innerRadius + halfW,
                        startIn - deltaIn, endIn + deltaIn, fCol);

                    AddSideEdge(vh, offset, innerRadius, startIn, outerRadius, startOut, halfW, fCol);
                    AddSideEdge(vh, offset, innerRadius, endIn, outerRadius, endOut, halfW, fCol);
                }
            }
        }

        private void AddCircleFilled(VertexHelper vh, Vector2 center, float radius, Color col, int steps)
        {
            int centerIdx = vh.currentVertCount;
            AddVertex(vh, center, col);
            for (int i = 0; i <= steps; i++)
            {
                float angle = (360f / steps) * i;
                AddVertex(vh, center + ClockDir(angle) * radius, col);
                if (i > 0)
                {
                    vh.AddTriangle(centerIdx, centerIdx + i, centerIdx + i + 1);
                }
            }
        }

        private void AddAnnularSectorFilled(VertexHelper vh, Vector2 center, float rIn, float rOut,
            float startIn, float endIn, float startOut, float endOut, Color col, int steps = 16)
        {
            int baseIdx = vh.currentVertCount;
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                float aIn = Mathf.Lerp(startIn, endIn, t);
                float aOut = Mathf.Lerp(startOut, endOut, t);

                AddVertex(vh, center + ClockDir(aOut) * rOut, col);
                AddVertex(vh, center + ClockDir(aIn) * rIn, col);

                if (i > 0)
                {
                    int v0 = baseIdx + (i - 1) * 2;
                    int v1 = v0 + 1;
                    int v2 = baseIdx + i * 2;
                    int v3 = v2 + 1;

                    vh.AddTriangle(v0, v2, v1);
                    vh.AddTriangle(v1, v2, v3);
                }
            }
        }

        private void AddArcRibbon(VertexHelper vh, Vector2 center, float rMin, float rMax,
            float startClockAngle, float endClockAngle, Color col, int steps = -1)
        {
            float span = Mathf.Abs(endClockAngle - startClockAngle);
            if (steps <= 0)
                steps = Mathf.Clamp(Mathf.CeilToInt(span * 0.8f), 12, 40);

            int baseIdx = vh.currentVertCount;
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                float angle = Mathf.Lerp(startClockAngle, endClockAngle, t);
                var dir = ClockDir(angle);

                AddVertex(vh, center + dir * rMax, col);
                AddVertex(vh, center + dir * rMin, col);

                if (i > 0)
                {
                    int v0 = baseIdx + (i - 1) * 2;
                    int v1 = v0 + 1;
                    int v2 = baseIdx + i * 2;
                    int v3 = v2 + 1;

                    vh.AddTriangle(v0, v2, v1);
                    vh.AddTriangle(v1, v2, v3);
                }
            }
        }

        private void AddSideEdge(VertexHelper vh, Vector2 center, float rIn, float angleIn,
            float rOut, float angleOut, float halfW, Color col)
        {
            Vector2 pIn  = center + ClockDir(angleIn) * rIn;
            Vector2 pOut = center + ClockDir(angleOut) * rOut;

            Vector2 dir = (pOut - pIn).normalized;
            Vector2 normal = new Vector2(dir.y, -dir.x) * halfW;

            Vector2 p0 = pIn - dir * halfW;
            Vector2 p1 = pOut + dir * halfW;

            int startIdx = vh.currentVertCount;
            AddVertex(vh, p0 - normal, col);
            AddVertex(vh, p0 + normal, col);
            AddVertex(vh, p1 + normal, col);
            AddVertex(vh, p1 - normal, col);

            vh.AddTriangle(startIdx, startIdx + 1, startIdx + 2);
            vh.AddTriangle(startIdx, startIdx + 2, startIdx + 3);
        }

        private void AddVertex(VertexHelper vh, Vector2 position, Color col)
        {
            var vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = col;
            vh.AddVert(vertex);
        }

        private static Vector2 ClockDir(float clockDegrees)
        {
            float rad = clockDegrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));
        }
    }

    // =============================================
    // ВІДЖЕТ СЛОТУ РАДІАЛЬНОГО МЕНЮ
    // Більші іконки (80x80), кругла підкладка активної стріли
    // =============================================
    public sealed class ProceduralSlotWidget
    {
        private GameObject? _root;
        private Image? _icon;
        private Text? _stackText;
        private GameObject? _activeBadgeObj;
        private Image? _activeBadgeImg;
        private GameObject? _durabilityBarObj;
        private RectTransform? _durabilityFillRect;
        private Image? _durabilityFill;
        private Vector2 _baseDir;
        private float _midRadius;

        // Effect highlight: a glow behind the icon in the colour of what the
        // item restores, split between colours for balanced food.
        private Image? _effectGlow;
        private readonly Color[] _effectColors = new Color[3];
        private ItemDrop.ItemData.SharedData? _effectShared;
        private bool _effectShown;
        private const float EffectGlowAlpha = 0.55f;

        public GameObject? Root => _root;

        public void Init(GameObject parent, int index)
        {
            _root = new GameObject($"ProceduralSlot_{index}");
            _root.transform.SetParent(parent.transform, false);

            var rect = _root.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot     = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(86f, 86f);

            // Кругла світло-голуба градієнтна підкладка активної стріли
            _activeBadgeObj = new GameObject("ActiveBadge");
            _activeBadgeObj.transform.SetParent(_root.transform, false);
            var badgeRect = _activeBadgeObj.AddComponent<RectTransform>();
            badgeRect.anchorMin = new Vector2(0.5f, 0.5f);
            badgeRect.anchorMax = new Vector2(0.5f, 0.5f);
            badgeRect.pivot     = new Vector2(0.5f, 0.5f);
            badgeRect.sizeDelta = new Vector2(106f, 106f);
            _activeBadgeImg = _activeBadgeObj.AddComponent<Image>();
            _activeBadgeImg.sprite = VanillaRadialSprites.RadialGlow;
            _activeBadgeImg.color = Color.white;
            _activeBadgeImg.raycastTarget = false;
            _activeBadgeObj.SetActive(false);

            // Сяйво кольору головного ефекту (здоров'я / витривалість / ейтр)
            var glowObj = new GameObject("EffectGlow");
            glowObj.transform.SetParent(_root.transform, false);
            var glowRect = glowObj.AddComponent<RectTransform>();
            glowRect.anchorMin = new Vector2(0.5f, 0.5f);
            glowRect.anchorMax = new Vector2(0.5f, 0.5f);
            glowRect.pivot     = new Vector2(0.5f, 0.5f);
            glowRect.sizeDelta = new Vector2(100f, 100f);
            _effectGlow = glowObj.AddComponent<Image>();
            _effectGlow.sprite = VanillaRadialSprites.WhiteGlow;
            _effectGlow.raycastTarget = false;
            glowObj.SetActive(false);

            // Більша іконка предмета (80x80)
            var iconObj = new GameObject("Icon");
            iconObj.transform.SetParent(_root.transform, false);
            var iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot     = new Vector2(0.5f, 0.5f);
            iconRect.sizeDelta = new Vector2(80f, 80f);
            _icon = iconObj.AddComponent<Image>();
            _icon.raycastTarget = false;

            // Смужка міцності
            _durabilityBarObj = new GameObject("DurabilityBar");
            _durabilityBarObj.transform.SetParent(_root.transform, false);
            var barRect = _durabilityBarObj.AddComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0.12f, 0.05f);
            barRect.anchorMax = new Vector2(0.88f, 0.13f);
            barRect.offsetMin = Vector2.zero;
            barRect.offsetMax = Vector2.zero;
            var barBg = _durabilityBarObj.AddComponent<Image>();
            barBg.color = new Color(0f, 0f, 0f, 0.85f);
            barBg.raycastTarget = false;

            var fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(_durabilityBarObj.transform, false);
            _durabilityFillRect = fillObj.AddComponent<RectTransform>();
            _durabilityFillRect.anchorMin = Vector2.zero;
            _durabilityFillRect.anchorMax = Vector2.one;
            _durabilityFillRect.offsetMin = Vector2.zero;
            _durabilityFillRect.offsetMax = Vector2.zero;
            _durabilityFill = fillObj.AddComponent<Image>();
            _durabilityFill.color = new Color(0.2f, 0.8f, 0.2f, 0.95f);
            _durabilityFill.raycastTarget = false;
            _durabilityBarObj.SetActive(false);

            // Кількість предметів
            var textObj = new GameObject("StackText");
            textObj.transform.SetParent(_root.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(1f, 0f);
            textRect.anchorMax = new Vector2(1f, 0f);
            textRect.pivot     = new Vector2(1f, 0f);
            textRect.anchoredPosition = new Vector2(-2f, 2f);
            textRect.sizeDelta = new Vector2(56f, 24f);
            _stackText = textObj.AddComponent<Text>();
            _stackText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            _stackText.fontSize = 16;
            _stackText.fontStyle = FontStyle.Bold;
            _stackText.alignment = TextAnchor.LowerRight;
            _stackText.color = Color.white;
            _stackText.raycastTarget = false;

            var outline = textObj.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }

        /// <summary>
        /// Colour the slot by what the item restores. Recomputed only when the
        /// slot shows a different item type, since Bind runs every frame.
        /// </summary>
        private void UpdateEffectHighlight(ItemDrop.ItemData? item)
        {
            bool enabled = Plugin.RadialEffectHighlight.Value;
            var shared = item?.m_shared;
            if (enabled == _effectShown && ReferenceEquals(shared, _effectShared)) return;
            _effectShared = shared;
            _effectShown = enabled;

            var effects = enabled ? ItemEffects.Of(item) : default;
            int n = effects.GetMainColors(_effectColors);
            if (_effectGlow == null) return;
            _effectGlow.gameObject.SetActive(n > 0);
            if (n == 1)
            {
                var c = _effectColors[0];
                _effectGlow.sprite = VanillaRadialSprites.WhiteGlow;
                _effectGlow.color = new Color(c.r, c.g, c.b, EffectGlowAlpha);
            }
            else if (n > 1)
            {
                // The colours are baked into the sprite; the tint only fades it.
                _effectGlow.sprite = VanillaRadialSprites.SplitGlow(_effectColors, n);
                _effectGlow.color = new Color(1f, 1f, 1f, EffectGlowAlpha);
            }
        }

        public void SetLayout(Vector2 dir, float midRadius)
        {
            _baseDir = dir;
            _midRadius = midRadius;
            if (_icon != null)
                _icon.rectTransform.sizeDelta = Vector2.one * Mathf.Max(1f, Plugin.RadialSlotSize.Value);
            UpdateNudge(0f);
        }

        public void UpdateNudge(float nudge)
        {
            if (_root == null) return;
            _root.transform.localPosition = (Vector3)_baseDir * (_midRadius + nudge);
        }

        public void SetPressScale(float scale)
        {
            if (_root == null) return;
            _root.transform.localScale = Vector3.one * scale;
        }

        public void SetActiveAmmo(bool active)
        {
            if (_activeBadgeObj != null)
                _activeBadgeObj.SetActive(active);
        }

        public void Bind(ItemDrop.ItemData? item, bool isActiveAmmo)
        {
            if (_root == null) return;
            if (item == null)
            {
                _root.SetActive(false);
                return;
            }

            // The slot holds an item, so the game wants it shown — but the
            // profile can still hide it.
            _root.SetActive(DynamicOverride.Visible(true, !HiddenByProfile));
            if (_icon != null)
            {
                _icon.sprite = item.GetIcon();
                _icon.color = Color.white;
            }

            UpdateEffectHighlight(item);

            if (_stackText != null)
            {
                int count = item.m_stack;
                var player = Player.m_localPlayer;
                if (player != null && item.m_shared != null)
                {
                    int total = player.GetInventory()?.CountItems(item.m_shared.m_name) ?? count;
                    if (total > count) count = total;
                }
                _stackText.text = count > 1 ? $"x{count}" : "";
            }

            SetActiveAmmo(isActiveAmmo);

            if (_durabilityBarObj != null && _durabilityFillRect != null && _durabilityFill != null)
            {
                if (item.m_shared != null && item.m_shared.m_useDurability)
                {
                    float maxDur = item.GetMaxDurability();
                    if (maxDur > 0f && item.m_durability < maxDur)
                    {
                        float durPercent = Mathf.Clamp01(item.m_durability / maxDur);
                        _durabilityBarObj.SetActive(true);
                        _durabilityFillRect.anchorMax = new Vector2(durPercent, 1f);
                        _durabilityFill.color = durPercent > 0.5f ? Color.green : (durPercent > 0.25f ? Color.yellow : Color.red);
                    }
                    else
                    {
                        _durabilityBarObj.SetActive(false);
                    }
                }
                else
                {
                    _durabilityBarObj.SetActive(false);
                }
            }
        }

        /// <summary>
        /// Set by the wheel when the layer profile disables this slot. The slot
        /// then stays hidden regardless of whether it holds an item, because
        /// Bind() and SetActive() are called from the game's own update and
        /// would otherwise switch it straight back on.
        /// </summary>
        public bool HiddenByProfile { get; set; }

        public void SetActive(bool active)
        {
            if (_root != null)
                _root.SetActive(DynamicOverride.Visible(active, !HiddenByProfile));
        }
    }

    // =============================================
    // МЕНЕДЖЕР ПРОЦЕДУРНОГО КОЛЕСА
    // =============================================
    public sealed class ProceduralRadialWheel
    {
        private GameObject? _root;
        private ProceduralRadialGraphic? _graphic;
        private RectTransform? _highlighter;
        private Image? _highlighterImage;
        private RectTransform? _cursorRoot;
        private RectTransform? _cursorTip;
        private Image? _cursorImage;
        private Text? _centerLabel;
        private readonly ItemInfoPanel _info = new ItemInfoPanel();
        private readonly List<ProceduralSlotWidget> _slots = new List<ProceduralSlotWidget>();

        private int _segments = 8;
        private int _itemCount = 0;
        private int _hoverIndex = -1;
        private float[] _sectorWeights = new float[16];
        private int _activeItemIndex = -1;
        private float _distance = 320f;
        private string _customCenterText = "";

        private float _cursorAngle;
        private float _highlighterAngle;
        private float _highlighterAlpha;

        /// <summary>
        /// This wheel's own layer profile. Each surface (heal-wheel /
        /// arrow-wheel) owns a separate instance, so their overrides never mix.
        /// </summary>
        private LayerProfile? _profile;

        /// <summary>Base values, captured before any profile is applied.</summary>
        private readonly NodeBaseline _baseline = new NodeBaseline();

        /// <summary>Semantic node id -> the GameObject it addresses.</summary>
        private readonly Dictionary<string, GameObject> _nodes =
            new Dictionary<string, GameObject>();

        /// <summary>The surface id this wheel was created with.</summary>
        private string _surfaceId = "";

        // ----- overrides for nodes the update loop rewrites every frame -----
        //
        // The highlighter, the cursor and the slots are not static layout: the
        // update loop sets their colour from .cfg, toggles them by hover/aim,
        // and Bind() re-enables a slot whenever it holds an item. Writing a
        // profile value onto them once (as ApplyProfile does for static nodes)
        // lasts exactly until the next frame.
        //
        // So for these the profile is not applied as a one-off write. It is
        // resolved into the fields below and combined with the live state at the
        // point of update:
        //   visible = gameWantsVisible && profileEnabled
        //   colour  = profileTint ?? cfgColour, with the animated alpha and the
        //             profile alpha multiplied together.
        // Geometry stays with the game — these nodes are positioned by angle
        // every frame, so a profile must not pin them.
        private bool _ovHighlighterEnabled = true;
        private Color? _ovHighlighterTint;
        private float _ovHighlighterAlpha = 1f;
        private bool _ovCursorEnabled = true;
        private Color? _ovCursorTint;
        private float _ovCursorAlpha = 1f;
        private Vector2? _ovCursorPosition;

        /// <summary>Per-slot "enabled" override; absent means the game decides.</summary>
        private readonly Dictionary<int, bool> _ovSlotEnabled = new Dictionary<int, bool>();

        /// <summary>
        /// True when the profile hides this slot, so Bind() must not re-enable
        /// it even though it holds an item.
        /// </summary>
        private bool SlotHiddenByProfile(int index) =>
            _ovSlotEnabled.TryGetValue(index, out var on) && !on;

        /// <summary>
        /// Pull the dynamic-node overrides out of the profile into the cached
        /// fields above. Called from ApplyProfile, on the main thread.
        /// </summary>
        private void ResolveDynamicOverrides()
        {
            _ovHighlighterEnabled = true; _ovHighlighterTint = null; _ovHighlighterAlpha = 1f;
            _ovCursorEnabled = true; _ovCursorTint = null; _ovCursorAlpha = 1f;
            _ovCursorPosition = null;
            _ovSlotEnabled.Clear();
            // This node is excluded from NodeBaseline because its position is dynamic.
            // Restore its static size explicitly when the override is removed.
            if (_cursorTip != null) {
                _cursorTip.sizeDelta = new Vector2(95f, 20f);
                _cursorTip.localPosition = Vector3.up * (Mathf.Max(10f, _distance - 86f) - 10f);
            }

            if (_profile == null) return;

            var hi = _profile.Resolve($"{_surfaceId}.highlighter");
            if (hi.Found)
            {
                if (hi.Enabled.HasValue) _ovHighlighterEnabled = hi.Enabled.Value;
                if (hi.Tint != null && hi.Tint.Length == 4)
                    _ovHighlighterTint = new Color(hi.Tint[0], hi.Tint[1], hi.Tint[2], hi.Tint[3]);
                if (hi.Alpha.HasValue) _ovHighlighterAlpha = hi.Alpha.Value;
            }

            // The chevron's tint lives on its tip, which is the graphic; the
            // root carries only visibility.
            var cur = _profile.Resolve($"{_surfaceId}.cursor");
            if (cur.Found && cur.Enabled.HasValue) _ovCursorEnabled = cur.Enabled.Value;

            var tip = _profile.Resolve($"{_surfaceId}.cursor.tip");
            if (tip.Found)
            {
                if (tip.Tint != null && tip.Tint.Length == 4)
                    _ovCursorTint = new Color(tip.Tint[0], tip.Tint[1], tip.Tint[2], tip.Tint[3]);
                if (tip.Alpha.HasValue) _ovCursorAlpha = tip.Alpha.Value;
                if (tip.Pos != null && tip.Pos.Length == 2)
                {
                    _ovCursorPosition = new Vector2(tip.Pos[0], tip.Pos[1]);
                    if (_cursorTip != null) _cursorTip.localPosition = _ovCursorPosition.Value;
                }

                // The editor offers `size` on the chevron, and unlike its
                // position (rewritten every frame from the sector radius) its
                // sizeDelta is set once at construction — so it is safe to write
                // here and stays put.
                if (tip.Size != null && tip.Size.Length == 2 && _cursorTip != null)
                    _cursorTip.sizeDelta = new Vector2(tip.Size[0], tip.Size[1]);
            }

            for (int i = 0; i < _slots.Count; i++)
            {
                var slot = _profile.Resolve($"{_surfaceId}.slot[{i}]");
                if (slot.Found && slot.Enabled.HasValue) _ovSlotEnabled[i] = slot.Enabled.Value;

                // The widget enforces it itself, since Bind() runs from the
                // game's update where this wheel is not in the call path.
                _slots[i].HiddenByProfile = SlotHiddenByProfile(i);
                if (_slots[i].HiddenByProfile) _slots[i].SetActive(false);
            }
        }

        private void RegisterNode(string nodeId, GameObject? target)
        {
            if (target == null) return;
            _nodes[nodeId] = target;
        }

        /// <summary>
        /// Lay this wheel's profile over what Rebuild just produced.
        ///
        /// Restore-then-apply: applying alone writes only the keys the profile
        /// names, so a removed key — or a deleted profile — would leave the old
        /// value on an already-built object and "reset" would move nothing.
        /// </summary>
        private void ApplyProfile()
        {
            if (_profile == null) return;

            // Nodes the update loop owns are handled by value, not by writing.
            ResolveDynamicOverrides();

            foreach (var pair in _nodes)
            {
                // Skip the dynamic ones. Writing to them here is pointless (the
                // next frame overwrites it) and actively harmful: Capture would
                // snapshot an angle-driven position and Restore would then pin
                // them there, freezing the animation.
                if (IsDynamicNode(pair.Key)) continue;

                _baseline.Capture(pair.Key, pair.Value);
                _baseline.Restore(pair.Key, pair.Value);
                // The centre label shows the hovered item's name and count, so
                // its styling is editable but its text belongs to the game.
                bool isGameText = pair.Key.EndsWith(".centerLabel");
                _profile.Apply(pair.Key, pair.Value, allowTextValue: !isGameText);
            }
        }

        /// <summary>
        /// Nodes whose visibility, colour or transform the update loop rewrites
        /// every frame. Their overrides are combined with the live state in
        /// UpdateMeshAndWidgets / Bind instead of being written once.
        /// </summary>
        private bool IsDynamicNode(string nodeId) =>
            nodeId == $"{_surfaceId}.highlighter"
            || nodeId == $"{_surfaceId}.cursor"
            || nodeId == $"{_surfaceId}.cursor.tip"
            || nodeId.StartsWith($"{_surfaceId}.slot[");

        /// <summary>
        /// Re-read the profile if the file changed, and re-apply if so.
        /// Called from the wheel's update on the main thread. While the wheel is
        /// open the change is held back: re-applying mid-interaction would move
        /// the sectors under the player's aim. It lands on the next rebuild.
        /// </summary>
        public bool PollProfile(ManualLogSource log, bool isOpen)
        {
            if (_profile == null) return false;
            if (!_profile.Poll(log)) return false;
            if (isOpen)
            {
                // Deferred: Rebuild() applies it when the wheel next opens.
                return false;
            }
            ApplyProfile();
            return true;
        }

        public int HoverIndex => _hoverIndex;
        public GameObject? Root => _root;

        public void Init(GameObject host, string name, Vector2 offset, string surfaceId)
        {
            if (host == null) return;
            if (_root != null) return;

            // Each wheel reads its own profile file; see docs/radial-surfaces.md.
            _surfaceId = surfaceId;
            _profile = new LayerProfile(surfaceId, Plugin.LogDebug);
            _nodes.Clear();
            _baseline.Clear();

            _root = new GameObject(name);
            _root.transform.SetParent(host.transform, false);

            var rect = _root.AddComponent<RectTransform>();
            rect.anchorMin        = new Vector2(0.5f, 0.5f);
            rect.anchorMax        = new Vector2(0.5f, 0.5f);
            rect.pivot            = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = offset;
            rect.sizeDelta        = new Vector2(10f, 10f);
            RegisterNode($"{surfaceId}.root", _root);

            // Затемнення фону — повноекранний Image позаду всього
            var dim = new GameObject("Dim");
            dim.transform.SetParent(_root.transform, false);
            var dimRect = dim.AddComponent<RectTransform>();
            dimRect.anchorMin = new Vector2(0.5f, 0.5f);
            dimRect.anchorMax = new Vector2(0.5f, 0.5f);
            dimRect.pivot     = new Vector2(0.5f, 0.5f);
            dimRect.sizeDelta = new Vector2(4000f, 4000f);
            var dimImg = dim.AddComponent<Image>();
            dimImg.color         = new Color(0f, 0f, 0f, 0.35f);
            dimImg.raycastTarget = false;
            RegisterNode($"{surfaceId}.dim", dim);

            // Процедурний меш
            var meshObj = new GameObject("ProceduralGraphic");
            meshObj.transform.SetParent(_root.transform, false);
            var meshRect = meshObj.AddComponent<RectTransform>();
            meshRect.anchorMin = new Vector2(0.5f, 0.5f);
            meshRect.anchorMax = new Vector2(0.5f, 0.5f);
            meshRect.pivot     = new Vector2(0.5f, 0.5f);
            meshRect.sizeDelta = new Vector2(1200f, 1200f);
            _graphic = meshObj.AddComponent<ProceduralRadialGraphic>();
            RegisterNode($"{surfaceId}.wheel", meshObj);
            _graphic.raycastTarget = false;

            // Плетінка-орнамент (Highlighter) з оригінальним спрайтом гри T_radialHighlighter
            var hlObj = new GameObject("Highlighter");
            hlObj.transform.SetParent(_root.transform, false);
            _highlighter = hlObj.AddComponent<RectTransform>();
            _highlighter.anchorMin = new Vector2(0.5f, 0.5f);
            _highlighter.anchorMax = new Vector2(0.5f, 0.5f);
            _highlighter.pivot     = new Vector2(0.5f, 0.5f);
            _highlighter.sizeDelta = new Vector2(180f, 36f);
            _highlighterImage = hlObj.AddComponent<Image>();
            RegisterNode($"{surfaceId}.highlighter", hlObj);
            _highlighterImage.sprite = VanillaRadialSprites.Highlighter;
            _highlighterImage.color = Plugin.RadialFrameColor;
            _highlighterImage.raycastTarget = false;
            hlObj.SetActive(false);

            // Стрілка-індикатор (Cursor) з оригінальним спрайтом гри T_radialIndicator
            var curObj = new GameObject("Cursor");
            curObj.transform.SetParent(_root.transform, false);
            _cursorRoot = curObj.AddComponent<RectTransform>();
            RegisterNode($"{surfaceId}.cursor", curObj);
            _cursorRoot.anchorMin = new Vector2(0.5f, 0.5f);
            _cursorRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _cursorRoot.pivot     = new Vector2(0.5f, 0.5f);

            var tipObj = new GameObject("CursorTip");
            tipObj.transform.SetParent(_cursorRoot.transform, false);
            _cursorTip = tipObj.AddComponent<RectTransform>();
            _cursorTip.anchorMin = new Vector2(0.5f, 0.5f);
            _cursorTip.anchorMax = new Vector2(0.5f, 0.5f);
            _cursorTip.pivot     = new Vector2(0.5f, 0.5f);
            _cursorTip.sizeDelta = new Vector2(95f, 20f);
            _cursorImage = tipObj.AddComponent<Image>();
            RegisterNode($"{surfaceId}.cursor.tip", tipObj);
            _cursorImage.sprite = VanillaRadialSprites.Indicator;
            _cursorImage.color = Plugin.RadialFrameColor;
            _cursorImage.raycastTarget = false;
            curObj.SetActive(false);

            // Центральний підпис
            var labelObj = new GameObject("CenterLabel");
            labelObj.transform.SetParent(_root.transform, false);
            var cRect = labelObj.AddComponent<RectTransform>();
            cRect.anchorMin        = new Vector2(0.5f, 0.5f);
            cRect.anchorMax        = new Vector2(0.5f, 0.5f);
            cRect.pivot            = new Vector2(0.5f, 0.5f);
            cRect.anchoredPosition = Vector2.zero;
            cRect.sizeDelta        = new Vector2(340f, 60f);
            _centerLabel = labelObj.AddComponent<Text>();
            RegisterNode($"{surfaceId}.centerLabel", labelObj);
            _centerLabel.fontSize  = 20;
            _centerLabel.fontStyle = FontStyle.Bold;
            _centerLabel.alignment = TextAnchor.MiddleCenter;
            _centerLabel.color     = new Color(0.95f, 0.92f, 0.84f);
            _centerLabel.raycastTarget = false;

            Font? averia = null;
            foreach (var f in Resources.FindObjectsOfTypeAll<Font>())
            {
                if (f != null && f.name.IndexOf("Averia", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    averia = f;
                    break;
                }
            }
            _centerLabel.font = averia != null ? averia : Resources.GetBuiltinResource<Font>("Arial.ttf");

            var outline = labelObj.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.95f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            _root.SetActive(false);
        }

        public static float GetElementsDistance()
        {
            float r = Plugin.RadialRadius.Value;
            return r < 200f ? 320f : r;
        }

        public void Rebuild(List<ItemDrop.ItemData> items, int activeItemIndex = -1)
        {
            if (_root == null) return;

            _distance = GetElementsDistance();
            _itemCount = items.Count;
            _activeItemIndex = activeItemIndex;

            // Мінімум 3 сектори (не штучні 4), максимум 16
            _segments = Mathf.Clamp(Mathf.Max(_itemCount, 3), 3, 16);

            if (_sectorWeights.Length < _segments)
                _sectorWeights = new float[_segments];

            for (int i = 0; i < _segments; i++)
                _sectorWeights[i] = 0f;

            _slots.RemoveAll(s => s == null || s.Root == null);

            while (_slots.Count < _segments)
            {
                var w = new ProceduralSlotWidget();
                w.Init(_root, _slots.Count);
                // Sector ids are positional, not item-based: styling must not
                // follow whatever happens to be in that inventory slot today.
                RegisterNode($"{_surfaceId}.slot[{_slots.Count}]", w.Root);
                _slots.Add(w);
            }

            float step = 360f / _segments;
            float innerRadius = Mathf.Max(10f, _distance - 86f);
            float outerRadius = _distance + 94f;
            float midRadius = (innerRadius + outerRadius) * 0.5f;

            // Not a profile node on purpose: NodeBaseline would snapshot and
            // restore its visibility, which the panel toggles at runtime.
            // Position and size come from the ItemInfo* CFG keys instead.
            if (_info.EnsureCreated(_root.transform))
                _info.Layout(outerRadius);

            for (int i = 0; i < _slots.Count; i++)
            {
                if (i < _segments)
                {
                    float midAngle = UIMath.Mod(i * step, 360f);
                    var dir = ClockDir(midAngle);
                    _slots[i].SetLayout(dir, midRadius);

                    if (i < _itemCount)
                    {
                        _slots[i].Bind(items[i], i == _activeItemIndex);
                    }
                    else
                    {
                        _slots[i].Bind(null, false);
                    }
                }
                else
                {
                    _slots[i].SetActive(false);
                }
            }

            // Позиція шеврона-вказівника на внутрішньому радіусі секторів
            if (_cursorTip != null)
            {
                _cursorTip.localPosition = _ovCursorPosition.HasValue
                    ? (Vector3)_ovCursorPosition.Value : Vector3.up * (innerRadius - 10f);
            }

            if (_highlighterImage != null)
            {
                _highlighterImage.sprite = VanillaRadialSprites.Highlighter;
            }
            if (_cursorImage != null)
            {
                _cursorImage.sprite = VanillaRadialSprites.Indicator;
            }

            _hoverIndex = -1;
            _highlighterAlpha = 0f;
            _customCenterText = "";
            UpdateMeshAndWidgets();

            // The base layout and the .cfg are in place, so the profile goes on
            // top. Doing it here rather than during Init means newly created
            // sector widgets are covered too.
            ApplyProfile();
        }

        public void SetActiveAmmoIndex(int index)
        {
            _activeItemIndex = index;
            for (int i = 0; i < _slots.Count; i++)
            {
                _slots[i].SetActiveAmmo(i == _activeItemIndex);
            }
        }

        public void SetCustomCenterText(string text)
        {
            _customCenterText = text;
            if (_centerLabel != null) _centerLabel.text = text;
        }

        public void UpdateHover(Vector2 aim, float deadZone, List<ItemDrop.ItemData> items)
        {
            float step = 360f / _segments;

            if (items.Count == 0)
            {
                _hoverIndex = -1;
                SetCustomCenterText(_customCenterText);
                for (int i = 0; i < _segments; i++)
                    _sectorWeights[i] = Mathf.MoveTowards(_sectorWeights[i], 0f, HoverFadeStep());
                if (_cursorRoot != null) _cursorRoot.gameObject.SetActive(false);
                if (_highlighter != null) _highlighter.gameObject.SetActive(false);
                _info.SetItem(null);
                _info.Tick();
                UpdateMeshAndWidgets();
                return;
            }

            _hoverIndex = RadialSelection.HoverIndex(aim.x, aim.y, deadZone, _segments, items.Count);
            _info.SetItem(_hoverIndex >= 0 && _hoverIndex < items.Count ? items[_hoverIndex] : null);
            _info.Tick();

            // Плавні анімації переходу для кожного сегмента
            const float nudgeDist = 20f;
            for (int i = 0; i < _segments; i++)
            {
                float target = (i == _hoverIndex) ? 1f : 0f;
                _sectorWeights[i] = Mathf.MoveTowards(_sectorWeights[i], target, HoverFadeStep());

                if (i < _slots.Count)
                {
                    if (i < items.Count)
                    {
                        _slots[i].Bind(items[i], i == _activeItemIndex);
                    }
                    _slots[i].UpdateNudge(nudgeDist * _sectorWeights[i]);
                }
            }

            // Центральний підпис
            if (_hoverIndex >= 0 && _hoverIndex < items.Count)
            {
                var it = items[_hoverIndex];
                string name = Localization.instance != null
                    ? Localization.instance.Localize(it.m_shared.m_name)
                    : it.m_shared.m_name;
                string activeTag = (_hoverIndex == _activeItemIndex) ? GameText.ActiveTag : "";
                string text = it.m_stack > 1 ? $"{name}{activeTag}  x{it.m_stack}" : $"{name}{activeTag}";
                if (_centerLabel != null) _centerLabel.text = text;
            }
            else
            {
                if (_centerLabel != null) _centerLabel.text = _customCenterText;
            }

            // Плетінка-орнамент: плавне ковзання, згасання і відступ +7px (+15px від зовнішнього радіусу)
            if (_highlighter != null)
            {
                float targetAlpha = (_hoverIndex >= 0) ? 1f : 0f;
                _highlighterAlpha = Mathf.MoveTowards(_highlighterAlpha, targetAlpha, HoverFadeStep());

                if (_highlighterAlpha > 0.01f)
                {
                    // Snap, never slide. The ornament marks the selected sector,
                    // so its angle IS that sector's centre — set outright, with
                    // no MoveTowardsAngle between the old and new centre.
                    //
                    // Sliding produced frames where the ornament sat between two
                    // sectors, pointing at a slot that was not selected, and on
                    // the 0 <-> last transition it swept across every sector in
                    // between. The reference artifact has no such state: it
                    // renders from dirFor(hoverTarget, count) directly.
                    //
                    // The angle is kept when nothing is hovered so the ornament
                    // fades out where it was rather than snapping to 0 first.
                    if (_hoverIndex >= 0)
                        _highlighterAngle = UIMath.Mod(_hoverIndex * step, 360f);

                    var dir = ClockDir(_highlighterAngle);
                    float currentW = (_hoverIndex >= 0 && _hoverIndex < _sectorWeights.Length) ? _sectorWeights[_hoverIndex] : 0f;
                    float knotRadius = _distance + Plugin.RadialOrnamentOffset.Value + (nudgeDist * currentW);

                    _highlighter.localPosition = (Vector3)dir * knotRadius;
                    _highlighter.localRotation = Quaternion.Euler(0f, 0f, -_highlighterAngle);

                    // Profile tint replaces the .cfg colour; the animated fade
                    // and the profile's alpha multiply, so a dimmed ornament
                    // still fades in and out with the hover.
                    _highlighterImage!.color = DynamicOverride.Tint(
                        Plugin.RadialFrameColor, _ovHighlighterTint,
                        _highlighterAlpha, _ovHighlighterAlpha);

                    // Hidden in the profile means hidden, even while hovering.
                    bool showHighlighter = DynamicOverride.Visible(true, _ovHighlighterEnabled);
                    _highlighter.gameObject.SetActive(showHighlighter);
                    if (showHighlighter) _highlighter.SetAsLastSibling();
                }
                else
                {
                    _highlighter.gameObject.SetActive(false);
                }
            }

            // The chevron is a direction indicator, separate from the visible game cursor.
            if (_cursorRoot != null)
            {
                bool showCursor = DynamicOverride.Visible(
                    aim.sqrMagnitude > 0f && aim.magnitude >= deadZone, _ovCursorEnabled);
                _cursorRoot.gameObject.SetActive(showCursor);
                if (showCursor)
                {
                    _cursorAngle = UIMath.DirectionToAngleDegrees(aim);

                    _cursorRoot.localRotation = Quaternion.Euler(0f, 0f, -_cursorAngle);

                    // Same contract as the highlighter. The chevron has no fade
                    // of its own, so the animation factor is 1.
                    _cursorImage!.color = DynamicOverride.Tint(
                        Plugin.RadialFrameColor, _ovCursorTint, 1f, _ovCursorAlpha);
                    _cursorRoot.SetAsLastSibling();
                }
            }

            if (_centerLabel != null) _centerLabel.transform.SetAsLastSibling();

            UpdateMeshAndWidgets();
        }

        public void SnapHover(int index)
        {
            _hoverIndex = index;
            for (int i = 0; i < _segments; i++)
                _sectorWeights[i] = (i == index) ? 1f : 0f;

            if (index >= 0)
            {
                float step = 360f / _segments;
                _cursorAngle = UIMath.Mod(index * step, 360f);
                _highlighterAngle = _cursorAngle;
                _highlighterAlpha = 1f;
                // Preserve profile overrides during the arrow selection confirmation.
                if (_cursorRoot != null)
                {
                    _cursorRoot.localRotation = Quaternion.Euler(0f, 0f, -_cursorAngle);
                    _cursorImage!.color = DynamicOverride.Tint(
                        Plugin.RadialFrameColor, _ovCursorTint, 1f, _ovCursorAlpha);
                    _cursorRoot.gameObject.SetActive(
                        DynamicOverride.Visible(true, _ovCursorEnabled));
                }
                if (_highlighter != null)
                {
                    var dir = ClockDir(_highlighterAngle);
                    _highlighter.localPosition = (Vector3)dir * (_distance + Plugin.RadialOrnamentOffset.Value + 20f);
                    _highlighter.localRotation = Quaternion.Euler(0f, 0f, -_highlighterAngle);
                    _highlighterImage!.color = DynamicOverride.Tint(
                        Plugin.RadialFrameColor, _ovHighlighterTint,
                        _highlighterAlpha, _ovHighlighterAlpha);
                    _highlighter.gameObject.SetActive(
                        DynamicOverride.Visible(true, _ovHighlighterEnabled));
                }
            }
            UpdateMeshAndWidgets();
        }

        // Click feedback: the pressed slot dips and springs back, and its sector
        // flashes — gold when the item was used, red when the game refused it.
        private const float PressDuration = 0.22f;
        private static readonly Color PressOkColor   = new Color(1f, 0.78f, 0.40f, 0.55f);
        private static readonly Color PressFailColor = new Color(0.85f, 0.12f, 0.08f, 0.60f);
        private int   _pressIndex = -1;
        private float _pressStart;
        private bool  _pressOk;
        private float _pressFlash;

        public void PressFeedback(int index, bool success)
        {
            if (_pressIndex >= 0 && _pressIndex < _slots.Count)
                _slots[_pressIndex].SetPressScale(1f);
            _pressIndex = index;
            _pressStart = Time.unscaledTime;
            _pressOk = success;
            UpdateMeshAndWidgets();
        }

        private void TickPress()
        {
            if (_pressIndex < 0) return;
            float t = (Time.unscaledTime - _pressStart) / PressDuration;
            bool slotValid = _pressIndex < _slots.Count;
            if (t >= 1f)
            {
                if (slotValid) _slots[_pressIndex].SetPressScale(1f);
                _pressIndex = -1;
                _pressFlash = 0f;
                return;
            }

            _pressFlash = 1f - t * t;
            // A refused use gets no "press", only the red flash.
            if (slotValid)
                _slots[_pressIndex].SetPressScale(_pressOk ? 1f - 0.18f * Mathf.Sin(t * Mathf.PI) : 1f);
        }

        private void ResetPress()
        {
            if (_pressIndex >= 0 && _pressIndex < _slots.Count)
                _slots[_pressIndex].SetPressScale(1f);
            _pressIndex = -1;
            _pressFlash = 0f;
        }

        private static float HoverFadeStep() => Plugin.RadialHoverSpeed.Value <= 0f
            ? 1f : Time.unscaledDeltaTime / Plugin.RadialHoverSpeed.Value;

        private void UpdateMeshAndWidgets()
        {
            TickPress();
            if (_graphic != null)
            {
                Color unselectedCol = new Color(0.03f, 0.03f, 0.03f, 0.92f);
                Color emptyCol = new Color(0.02f, 0.02f, 0.02f, Mathf.Clamp01(Plugin.RadialEmptyAlpha.Value));
                Color hoverCol = Plugin.RadialTintBackground.Value ? Plugin.RadialHoverColor : unselectedCol;

                _graphic.UpdateState(
                    _segments,
                    _itemCount,
                    _sectorWeights,
                    _distance,
                    unselectedCol,
                    emptyCol,
                    hoverCol,
                    Plugin.RadialFrameColor,
                    Plugin.RadialFrameWidth.Value,
                    _pressIndex,
                    _pressFlash,
                    _pressOk ? PressOkColor : PressFailColor
                );
            }
        }

        private int _openedFrame = -1;

        private Camera? EventCamera
        {
            get
            {
                var canvas = _root?.GetComponentInParent<Canvas>();
                return canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            }
        }

        public Vector2 GetMouseAim()
        {
            if (_root == null || Time.frameCount == _openedFrame) return Vector2.zero;
            var rect = (RectTransform)_root.transform;
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rect, ZInput.pointerPosition, EventCamera, out var local) ? local : Vector2.zero;
        }

        public void SetActive(bool active)
        {
            if (_root != null)
            {
                _root.SetActive(active);
                GameCamera.instance?.UpdateMouseCapture();
                if (active)
                {
                    _info.Show();
                    _openedFrame = Time.frameCount;
                    if (ZInput.IsMouseActive())
                        ZInput.SetMousePosition(RectTransformUtility.WorldToScreenPoint(EventCamera, _root.transform.position));
                }
                if (!active)
                {
                    _info.Hide();
                    ResetPress();
                    _hoverIndex = -1;
                    _highlighterAlpha = 0f;
                    for (int i = 0; i < _sectorWeights.Length; i++)
                        _sectorWeights[i] = 0f;
                    if (_highlighter != null) _highlighter.gameObject.SetActive(false);
                    if (_cursorRoot != null) _cursorRoot.gameObject.SetActive(false);
                    UpdateMeshAndWidgets();
                }
            }
        }

        private static Vector2 ClockDir(float clockDegrees)
        {
            float rad = clockDegrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));
        }
    }

    // =============================================
    // РАДІАЛЬНЕ МЕНЮ ХІЛОК (у стилі CS:GO)
    // =============================================
    public static class QuickHealRadial
    {
        /// <summary>Editor surface id; see docs/radial-surfaces.md.</summary>
        public const string SurfaceId = "heal-wheel";

        private const int   MaxSlots    = 16;

        private static ProceduralRadialWheel? _wheel;
        private static readonly List<ItemDrop.ItemData> _items = new List<ItemDrop.ItemData>();

        private static bool    _open;
        private static Vector2 _aim = Vector2.zero;
        private static KeyCode _activeKey = KeyCode.None;
        private static bool    _slowedTime;
        // Something was used by click during this opening.
        private static bool    _usedByClick;

        public static bool IsOpen => _open && _wheel != null && _wheel.Root != null;

        public static void TryCreate(Hud hud)
        {
            if (hud == null || hud.gameObject == null) return;
            if (_wheel != null && _wheel.Root != null) return;

            _wheel = new ProceduralRadialWheel();
            _wheel.Init(hud.gameObject, "HeroQuickHealRadial",
                new Vector2(Plugin.HealOffsetX.Value, Plugin.HealOffsetY.Value),
                SurfaceId);

            Plugin.LogDebug($"QuickHealRadial: створено процедурне колесо хілок на HUD.");
        }

        public static void UpdateRadial()
        {
            if (_wheel == null || _wheel.Root == null)
            {
                if (Hud.instance != null)
                    TryCreate(Hud.instance);
                if (_wheel == null || _wheel.Root == null)
                    return;
            }

            // One timestamp check per frame on the main thread. While the wheel
            // is open the change is deferred to the next rebuild, so sectors
            // never move under the player's aim mid-interaction.
            _wheel.PollProfile(Plugin.Log, _open);

            // Either key opens the wheel; the one that opened it closes it.
            if (!_open && Plugin.CanUseHotkeys() && !ArrowRadial.IsOpen)
            {
                var key1 = Plugin.RadialKey.Value;
                var key2 = Plugin.RadialKeySecondary.Value;
                KeyCode pressed = KeyCode.None;
                if (key1 != KeyCode.None && Plugin.KeyPressed(key1)) pressed = key1;
                else if (key2 != KeyCode.None && Plugin.KeyPressed(key2)) pressed = key2;

                if (pressed != KeyCode.None)
                {
                    _activeKey = pressed;
                    Open();
                }
            }

            if (!_open) return;

            if (!Plugin.CanUseHotkeys())
            {
                _activeKey = KeyCode.None;
                Close(false);
                return;
            }

            UpdateAim();
            _wheel.UpdateHover(_aim, Plugin.RadialDeadZone.Value, _items);

            var clickKey = Plugin.RadialClickKey.Value;
            if (Plugin.RadialClickToUse.Value && clickKey != KeyCode.None && clickKey != _activeKey
                && Plugin.KeyPressed(clickKey))
            {
                UseHovered();
            }

            bool shouldClose = false;
            if (_activeKey != KeyCode.None)
            {
                shouldClose = Plugin.KeyReleased(_activeKey) || !Plugin.KeyHeld(_activeKey);
            }

            if (shouldClose)
            {
                _activeKey = KeyCode.None;
                // After a click-use the release only closes: applying the
                // hovered slot again would double-eat the item just clicked.
                Close(!_usedByClick);
            }
        }

        /// <summary>
        /// Use the hovered item without closing the wheel. When the stack runs
        /// out the sectors are rebuilt so the slot picks up the next stack of
        /// the same item or drops out.
        /// </summary>
        private static void UseHovered()
        {
            if (_wheel == null || _wheel.Root == null) return;
            int index = _wheel.HoverIndex;
            if (index < 0 || index >= _items.Count) return;

            var player = Player.m_localPlayer;
            var item   = _items[index];
            if (player == null || item == null) return;

            _usedByClick = true;
            var inv = player.GetInventory();
            int before = inv.CountItems(item.m_shared.m_name);
            UseItem(player, item);
            // Vanilla gives no result; a shrinking count is the only reliable
            // sign that the item was actually consumed.
            _wheel.PressFeedback(index, inv.CountItems(item.m_shared.m_name) < before);

            if (!inv.ContainsItem(item))
            {
                RebuildItems();
                if (_items.Count == 0)
                    _wheel.SetCustomCenterText(GameText.NoConsumables);
            }
        }

        /// <summary>
        /// True while the wheel is inside vanilla UseItem with ConsumeAnimation
        /// off; SuppressEatAnimationPatch drops the "eat" trigger and the
        /// in-hand item for exactly that call. Everything else in UseItem —
        /// consumption, sound, vanilla checks — runs untouched.
        /// </summary>
        internal static bool SuppressEatAnimation { get; private set; }

        private static void UseItem(Player player, ItemDrop.ItemData item)
        {
            bool animate = Plugin.RadialConsumeAnimation.Value;
            Plugin.LogDebug($"QuickHealRadial: використання {item.m_shared.m_name}, ConsumeAnimation={animate}");

            SuppressEatAnimation = !animate;
            try
            {
                player.UseItem(player.GetInventory(), item, false);
            }
            finally
            {
                SuppressEatAnimation = false;
            }
        }

        private static void Open()
        {
            if (_wheel == null || _wheel.Root == null) return;
            RebuildItems();
            _open = true;
            _aim  = Vector2.zero;

            _wheel.SetActive(true);
            if (_items.Count == 0)
                _wheel.SetCustomCenterText(GameText.NoConsumables);

            if (Plugin.RadialSlowMotion.Value)
            {
                Time.timeScale = 0.25f;
                _slowedTime = true;
            }
        }

        private static void Close(bool apply)
        {
            if (_wheel != null && _wheel.Root != null && apply && _wheel.HoverIndex >= 0 && _wheel.HoverIndex < _items.Count)
            {
                var player = Player.m_localPlayer;
                var item   = _items[_wheel.HoverIndex];
                if (player != null && item != null)
                {
                    UseItem(player, item);
                }
            }

            _open = false;
            _usedByClick = false;
            _activeKey = KeyCode.None;
            if (_wheel != null && _wheel.Root != null) _wheel.SetActive(false);
            RestoreTimeScale();
        }

        /// <summary>
        /// Undo our own slow-motion. Keyed off the flag set when we slowed time,
        /// not off the current config value: toggling SlowMotion off while the
        /// wheel is open would otherwise leave the game running at 0.25x.
        /// </summary>
        private static void RestoreTimeScale()
        {
            if (!_slowedTime) return;
            _slowedTime = false;
            Time.timeScale = 1f;
        }

        /// <summary>
        /// Shut the wheel without using anything. Called on death, scene change,
        /// plugin unload and after an exception, because <see cref="IsOpen"/>
        /// drives the input patches — a stale open flag locks the player's
        /// controls.
        /// </summary>
        public static void ForceClose()
        {
            _open = false;
            _usedByClick = false;
            _activeKey = KeyCode.None;
            if (_wheel != null && _wheel.Root != null) _wheel.SetActive(false);
            RestoreTimeScale();
        }

        private static void UpdateAim()
        {
            _aim = _wheel?.GetMouseAim() ?? Vector2.zero;
        }

        private static void RebuildItems()
        {
            _items.Clear();
            var player = Player.m_localPlayer;
            var inv    = player != null ? player.GetInventory() : null;
            if (inv != null)
            {
                var uniqueMap = new Dictionary<string, ItemDrop.ItemData>();

                foreach (var item in inv.GetAllItems())
                {
                    if (item?.m_shared == null) continue;
                    if (item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Consumable) continue;
                    if (!Plugin.RadialIncludeAllConsumables.Value && !IsPotionOrHealing(item)) continue;

                    string key = item.m_shared.m_name;
                    if (!uniqueMap.ContainsKey(key))
                    {
                        uniqueMap[key] = item;
                    }
                }

                var candidateList = new List<ItemDrop.ItemData>(uniqueMap.Values);
                candidateList.Sort((a, b) =>
                {
                    int prioA = GetItemPriority(a);
                    int prioB = GetItemPriority(b);
                    if (prioA != prioB) return prioA.CompareTo(prioB);
                    return string.Compare(a.m_shared.m_name, b.m_shared.m_name, System.StringComparison.Ordinal);
                });

                for (int i = 0; i < candidateList.Count && i < MaxSlots; i++)
                {
                    _items.Add(candidateList[i]);
                }
            }

            if (_wheel != null && _wheel.Root != null)
                _wheel.Rebuild(_items);
        }

        private static bool IsPotionOrHealing(ItemDrop.ItemData item)
        {
            var sh = item?.m_shared;
            if (sh == null) return false;
            if (sh.m_itemType != ItemDrop.ItemData.ItemType.Consumable) return false;

            // Будь-яке зілля чи медовуха (має статус-ефект споживання)
            if (sh.m_consumeStatusEffect != null) return true;

            // Будь-яка їжа
            if (sh.m_food > 0f) return true;

            return false;
        }

        private static int GetItemPriority(ItemDrop.ItemData item)
        {
            var sh = item.m_shared;
            if (sh == null) return 999;

            var status = sh.m_consumeStatusEffect;
            if (status != null)
            {
                var se = status as SE_Stats;
                string n = status.name != null ? status.name.ToLowerInvariant() : "";
                string iname = sh.m_name != null ? sh.m_name.ToLowerInvariant() : "";

                // 1. Хілки (Health mead)
                if ((se != null && (se.m_healthOverTime > 0f || se.m_healthUpFront > 0f || se.m_healthRegenMultiplier > 1f))
                    || n.Contains("health") || iname.Contains("health"))
                {
                    if (n.Contains("major") || iname.Contains("major")) return 10;
                    if (n.Contains("medium") || iname.Contains("medium")) return 11;
                    return 12;
                }

                // 2. Стаміна (Stamina mead)
                if ((se != null && (se.m_staminaOverTime > 0f || se.m_staminaUpFront > 0f || se.m_staminaRegenMultiplier > 1f))
                    || n.Contains("stamina") || iname.Contains("stamina"))
                {
                    if (n.Contains("medium") || iname.Contains("medium")) return 20;
                    if (n.Contains("minor") || iname.Contains("minor")) return 21;
                    return 22;
                }

                // 3. Ейтр (Eitr mead)
                if ((se != null && (se.m_eitrOverTime > 0f || se.m_eitrUpFront > 0f || se.m_eitrRegenMultiplier > 1f))
                    || n.Contains("eitr") || iname.Contains("eitr"))
                {
                    return 30;
                }

                // 4. Опір та захист (Resistance meads: Poison, Frost, Fire)
                if (n.Contains("poison") || iname.Contains("poison")) return 40;
                if (n.Contains("frost") || iname.Contains("frost")) return 41;
                if (n.Contains("fire") || iname.Contains("fire")) return 42;

                // 5. Інші зілля (Tasty mead тощо)
                return 50;
            }

            // 6. Їжа (Food) - впорядкована за поживною цінністю (HP + стаміна)
            if (sh.m_food > 0f)
            {
                return 100 - Mathf.RoundToInt(sh.m_food + sh.m_foodStamina);
            }

            return 200;
        }
    }

    // =============================================
    // РАДІАЛЬНЕ МЕНЮ СТРІЛ
    // =============================================
    public static class ArrowRadial
    {
        /// <summary>Editor surface id; see docs/radial-surfaces.md.</summary>
        public const string SurfaceId = "arrow-wheel";

        private const int MaxSlots = 16;

        private static ProceduralRadialWheel? _wheel;
        private static readonly List<ItemDrop.ItemData> _items = new List<ItemDrop.ItemData>();

        private static bool    _open;
        private static Vector2 _aim = Vector2.zero;

        private static bool  _keyHeld;
        private static float _holdTimer;
        private static bool  _heldKeyHasGameAction;
        private static string? _lastHoldFailReason;
        private static KeyCode _activeKey = KeyCode.None;

        private static float _closeAt;
        private static int _selectedIndex = -1;

        public static bool IsOpen => _open && _wheel != null && _wheel.Root != null;

        private static void LogHoldFailOnce(string reason)
        {
            if (reason == _lastHoldFailReason) return;
            _lastHoldFailReason = reason;
            Plugin.LogDebug($"ArrowRadial: поріг утримання пройдено, але меню не відкрилось — {reason}");
        }

        public static void TryCreate(Hud hud)
        {
            if (hud == null || hud.gameObject == null) return;
            if (_wheel != null && _wheel.Root != null) return;
            _wheel = new ProceduralRadialWheel();
            _wheel.Init(hud.gameObject, "HeroArrowRadial",
                new Vector2(Plugin.ArrowRadialOffsetX.Value, Plugin.ArrowRadialOffsetY.Value),
                SurfaceId);

            Plugin.LogDebug($"ArrowRadial: створено процедурне колесо стріл на HUD.");
        }

        private static ItemDrop.ItemData? _lastBow;
        private static float _lastBowSeenTime;
        private const float BowMemorySeconds = 1.5f;

        private static bool IsBow(ItemDrop.ItemData? item)
        {
            return item != null && item.m_shared != null
                && item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Bow;
        }

        private static ItemDrop.ItemData? FindBow(Player player)
        {
            if (IsBow(player.LeftItem))  return player.LeftItem;
            if (IsBow(player.RightItem)) return player.RightItem;

            var hiddenLeft = AccessTools.FieldRefAccess<Humanoid, ItemDrop.ItemData>(
                player, "m_hiddenLeftItem");
            if (IsBow(hiddenLeft)) return hiddenLeft;

            var hiddenRight = AccessTools.FieldRefAccess<Humanoid, ItemDrop.ItemData>(
                player, "m_hiddenRightItem");
            if (IsBow(hiddenRight)) return hiddenRight;

            return null;
        }

        private static void TrackBow(Player? player)
        {
            if (player == null) return;

            var bow = FindBow(player);
            if (bow == null) return;

            _lastBow         = bow;
            _lastBowSeenTime = Time.unscaledTime;
        }

        private static bool IsBowEquipped(Player player)
        {
            return GetActiveBow(player) != null;
        }

        private static ItemDrop.ItemData? GetActiveBow(Player player)
        {
            var bow = FindBow(player);
            if (bow != null) return bow;

            if (_lastBow != null
                && Time.unscaledTime - _lastBowSeenTime <= BowMemorySeconds)
            {
                var inv = player.GetInventory();
                if (inv != null && inv.ContainsItem(_lastBow))
                    return _lastBow;
            }

            return null;
        }

        public static void UpdateRadial()
        {
            if (_wheel == null || _wheel.Root == null)
            {
                if (Hud.instance != null)
                    TryCreate(Hud.instance);
                if (_wheel == null || _wheel.Root == null)
                    return;
            }

            _wheel.PollProfile(Plugin.Log, _open);

            var key1 = Plugin.ArrowRadialKey.Value;
            var key2 = Plugin.ArrowRadialKeySecondary.Value;
            var player = Player.m_localPlayer;

            TrackBow(player);

            if (_closeAt > 0f)
            {
                if (Time.unscaledTime >= _closeAt)
                {
                    _closeAt = 0f;
                    if (_wheel != null && _wheel.Root != null)
                        _wheel.SetActive(false);
                }
                return;
            }

            if (_open)
            {
                if (!Plugin.CanUseHotkeys() || player == null || !IsBowEquipped(player))
                {
                    Close(false);
                    _keyHeld = false;
                    _activeKey = KeyCode.None;
                    return;
                }

                UpdateAim();
                _wheel.UpdateHover(_aim, Plugin.RadialDeadZone.Value, _items);

                bool released = false;
                if (_activeKey != KeyCode.None)
                    released = Plugin.KeyReleased(_activeKey) || !Plugin.KeyHeld(_activeKey);
                else
                    released = (key1 != KeyCode.None && Plugin.KeyReleased(key1)) ||
                               (key2 != KeyCode.None && Plugin.KeyReleased(key2));

                if (released)
                {
                    Close(true);
                    _keyHeld = false;
                    _activeKey = KeyCode.None;
                }
                return;
            }

            KeyCode heldKey = KeyCode.None;
            if (key1 != KeyCode.None && Plugin.KeyHeld(key1)) heldKey = key1;
            else if (key2 != KeyCode.None && Plugin.KeyHeld(key2)) heldKey = key2;

            if (heldKey == KeyCode.None)
            {
                _keyHeld   = false;
                _holdTimer = 0f;
                _lastHoldFailReason = null;
                _activeKey = KeyCode.None;
                return;
            }

            _activeKey = heldKey;
            if (!_keyHeld)
            {
                _keyHeld   = true;
                _holdTimer = 0f;
                // Looked up once per press, so a rebind in the game's
                // controls is picked up on the next press.
                _heldKeyHasGameAction = GameKeyBindings.HasGameAction(heldKey);
            }

            // The delay exists so a short press keeps the key's game action
            // (R hides the weapon). A key the game does not use opens at once.
            _holdTimer += Time.unscaledDeltaTime;
            if (_heldKeyHasGameAction && _holdTimer < Plugin.ArrowRadialHoldDelay.Value) return;

            if (!Plugin.CanUseHotkeys())
            {
                LogHoldFailOnce("CanUseHotkeys() == false");
                return;
            }
            if (QuickHealRadial.IsOpen)
            {
                LogHoldFailOnce("QuickHealRadial вже відкрите");
                return;
            }
            if (player == null)
            {
                LogHoldFailOnce("Player.m_localPlayer == null");
                return;
            }
            if (!IsBowEquipped(player))
            {
                LogHoldFailOnce("IsBowEquipped == false");
                return;
            }

            Open(player);
        }

        private static void Open(Player player)
        {
            if (_wheel == null || _wheel.Root == null) return;
            RebuildItems(player);
            _open = true;
            _aim  = Vector2.zero;
            _wheel.SetActive(true);
            if (_items.Count == 0)
                _wheel.SetCustomCenterText(GameText.NoAmmo);
        }

        // Надійний пошук активного боєприпасу
        private static ItemDrop.ItemData? GetCurrentAmmo(Player player)
        {
            if (player == null) return null;

            try
            {
                var ammo = player.GetAmmoItem();
                if (ammo != null) return ammo;
            }
            catch {}

            try
            {
                var ammoField = AccessTools.FieldRefAccess<Humanoid, ItemDrop.ItemData>(player, "m_ammoItem");
                if (ammoField != null) return ammoField;
            }
            catch {}

            var inv = player.GetInventory();
            if (inv != null)
            {
                foreach (var it in inv.GetAllItems())
                {
                    if (it != null && it.m_equipped &&
                        (it.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Ammo ||
                         it.m_shared.m_itemType == ItemDrop.ItemData.ItemType.AmmoNonEquipable))
                    {
                        return it;
                    }
                }
            }

            return null;
        }

        private static void Close(bool apply)
        {
            var player = Player.m_localPlayer;

            if (_wheel != null && _wheel.Root != null && apply && _wheel.HoverIndex >= 0 && _wheel.HoverIndex < _items.Count)
            {
                var item = _items[_wheel.HoverIndex];
                if (player != null && item != null)
                {
                    // Екіпіруємо обрану стрілу
                    player.EquipItem(item, true);

                    // Одразу перемикаємо круглу підкладку активної стріли на вибраний слот
                    _selectedIndex = _wheel.HoverIndex;
                    _wheel.SetActiveAmmoIndex(_selectedIndex);
                    _wheel.SnapHover(_selectedIndex);

                    _closeAt = Time.unscaledTime + Plugin.ArrowRadialPickDelay.Value;
                    _open    = false;
                    GameCamera.instance?.UpdateMouseCapture();
                    _activeKey = KeyCode.None;
                    RestoreHiddenBow(player);
                    return;
                }
            }

            if (player != null)
                RestoreHiddenBow(player);

            _open = false;
            _closeAt = 0f;
            _activeKey = KeyCode.None;
            if (_wheel != null && _wheel.Root != null) _wheel.SetActive(false);
        }

        /// <summary>
        /// Shut the wheel without equipping anything. Called on death, scene
        /// change, plugin unload and after an exception: <see cref="IsOpen"/>
        /// drives the input patches, so a stale open flag locks the controls.
        /// </summary>
        public static void ForceClose()
        {
            _open = false;
            _closeAt = 0f;
            _keyHeld = false;
            _holdTimer = 0f;
            _activeKey = KeyCode.None;
            if (_wheel != null && _wheel.Root != null) _wheel.SetActive(false);
        }

        private static System.Reflection.MethodInfo? _showHandItemsMethod;

        private static void RestoreHiddenBow(Player player)
        {
            var hiddenLeft = AccessTools.FieldRefAccess<Humanoid, ItemDrop.ItemData>(
                player, "m_hiddenLeftItem");
            var hiddenRight = AccessTools.FieldRefAccess<Humanoid, ItemDrop.ItemData>(
                player, "m_hiddenRightItem");

            if (!IsBow(hiddenLeft) && !IsBow(hiddenRight)) return;

            _showHandItemsMethod ??= AccessTools.Method(typeof(Humanoid), "ShowHandItems");
            _showHandItemsMethod?.Invoke(player, new object[] { false, true });
        }

        private static void UpdateAim()
        {
            _aim = _wheel?.GetMouseAim() ?? Vector2.zero;
        }

        private static void RebuildItems(Player player)
        {
            _items.Clear();

            var bow = GetActiveBow(player);
            string? ammoType = bow != null && bow.m_shared != null ? bow.m_shared.m_ammoType : null;

            var inv = player.GetInventory();
            if (inv != null && !string.IsNullOrEmpty(ammoType))
            {
                var uniqueMap = new Dictionary<string, ItemDrop.ItemData>();
                foreach (var item in inv.GetAllItems())
                {
                    if (item?.m_shared == null) continue;
                    var itype = item.m_shared.m_itemType;
                    if (itype != ItemDrop.ItemData.ItemType.Ammo &&
                        itype != ItemDrop.ItemData.ItemType.AmmoNonEquipable)
                        continue;
                    if (item.m_shared.m_ammoType != ammoType) continue;

                    string key = item.m_shared.m_name;
                    if (!uniqueMap.ContainsKey(key))
                    {
                        uniqueMap[key] = item;
                    }
                }

                foreach (var it in uniqueMap.Values)
                {
                    _items.Add(it);
                    if (_items.Count >= MaxSlots) break;
                }
            }

            _items.Sort((a, b) => string.CompareOrdinal(a.m_shared.m_name, b.m_shared.m_name));

            var currentAmmo = GetCurrentAmmo(player);
            _selectedIndex = -1;
            if (currentAmmo != null)
            {
                _selectedIndex = _items.FindIndex(it => it == currentAmmo || (it.m_shared != null && currentAmmo.m_shared != null && it.m_shared.m_name == currentAmmo.m_shared.m_name));
            }

            if (_wheel != null && _wheel.Root != null)
                _wheel.Rebuild(_items, _selectedIndex);
        }
    }
}
