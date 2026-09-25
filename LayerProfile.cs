using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace HeroRadialMenusMod
{
    /// <summary>
    /// Reads the layer profiles the editor writes
    /// (ValheimUiEditor/docs/layer-profile-contract.md) and applies them to the
    /// wheels.
    ///
    /// Unlike the other two mods this one owns **two** surfaces —
    /// <c>heal-wheel</c> and <c>arrow-wheel</c> — so every instance tracks its
    /// own file, its own cache and its own failure state. Nothing is shared
    /// between them; an override saved for one wheel can never reach the other.
    ///
    /// Precedence is base layout -> .cfg -> profile, so a profile is applied
    /// after the wheel has been built and after its settings have been
    /// computed. Applying earlier would simply be recomputed away.
    /// </summary>
    internal sealed class LayerProfile
    {
        public const int SchemaVersion = 1;
        private const string ModId = "com.heromod.valheim.radialmenus";

        private readonly string _surfaceId;
        private readonly Action<string>? _debugLog;
        private Dictionary<string, Dictionary<string, object?>>? _overrides;
        private string? _path;
        private DateTime _lastWrite;
        private bool _loadFailed;

        public LayerProfile(string surfaceId, Action<string>? debugLog = null)
        {
            _surfaceId = surfaceId;
            _debugLog = debugLog;
        }

        public bool HasOverrides => _overrides != null && _overrides.Count > 0;

        /// <summary>
        /// Where profiles live. Normally BepInEx's config directory; tests point
        /// it at a sandbox, since BepInEx.Paths is read-only outside the game.
        /// </summary>
        public static string? ProfileDirectoryOverride { get; set; }

        public string ProfilePath
        {
            get
            {
                if (_path != null) return _path;
                var dir = ProfileDirectoryOverride
                          ?? Path.Combine(BepInEx.Paths.ConfigPath, "HeroModUiProfiles");
                _path = Path.Combine(dir, $"{ModId}.{_surfaceId}.json");
                return _path;
            }
        }

        /// <summary>
        /// Re-read the file if it changed on disk. One timestamp check for the
        /// whole wheel, on the main thread — no watcher, and nothing per node.
        /// Returns true when the caller should re-apply.
        /// </summary>
        public bool Poll(ManualLogSource log)
        {
            try
            {
                if (!File.Exists(ProfilePath))
                {
                    if (_overrides == null) return false;
                    _overrides = null;      // profile deleted -> back to stock
                    _lastWrite = default;
                    _loadFailed = false;
                    _debugLog?.Invoke($"Профіль шарів ({_surfaceId}) видалено — колесо повернулось до штатного вигляду.");
                    return true;
                }

                var stamp = File.GetLastWriteTimeUtc(ProfilePath);
                if (stamp == _lastWrite) return false;
                _lastWrite = stamp;
                return Load(log);
            }
            catch (IOException)
            {
                // The editor may be mid-write; try again on the next poll.
                return false;
            }
        }

        /// <summary>
        /// Parse the profile. A broken or unknown-version file leaves the wheel
        /// exactly as it was built and is reported once, so a bad profile can
        /// never become an exception every frame.
        /// </summary>
        private bool Load(ManualLogSource log)
        {
            try
            {
                var json = File.ReadAllText(ProfilePath, Encoding.UTF8);
                var root = MiniJson.Parse(json) as Dictionary<string, object?>;
                if (root == null) throw new FormatException("кореневий елемент не є об'єктом");

                var version = ToInt(root.TryGetValue("schemaVersion", out var sv) ? sv : null, 0);
                if (version > SchemaVersion)
                {
                    if (!_loadFailed)
                        log.LogWarning($"Профіль шарів ({_surfaceId}) має версію {version}, " +
                                       $"підтримується {SchemaVersion}. Профіль проігноровано.");
                    _loadFailed = true;
                    _overrides = null;
                    return true;
                }

                var parsed = new Dictionary<string, Dictionary<string, object?>>();
                if (root.TryGetValue("overrides", out var ovRaw) && ovRaw is Dictionary<string, object?> ov)
                {
                    foreach (var pair in ov)
                    {
                        if (pair.Value is Dictionary<string, object?> props)
                            parsed[pair.Key] = props;
                    }
                }

                _overrides = parsed;
                _loadFailed = false;
                _debugLog?.Invoke($"Профіль шарів ({_surfaceId}) прочитано: {parsed.Count} вузлів з перевизначеннями.");
                return true;
            }
            catch (Exception ex)
            {
                if (!_loadFailed)
                    log.LogWarning($"Не вдалося прочитати профіль шарів ({_surfaceId}): {ex.Message}. " +
                                   "Колесо лишилось у штатному вигляді.");
                _loadFailed = true;
                _overrides = null;
                return true;
            }
        }

        // --------------------------------------------------------- applying

        /// <summary>
        /// Apply this node's overrides. Absent keys are left untouched, so the
        /// caller must restore the captured baseline first — see NodeBaseline.
        /// </summary>
        public void Apply(string nodeId, GameObject? target, bool allowTextValue = true)
        {
            if (_overrides == null || target == null) return;
            if (!_overrides.TryGetValue(nodeId, out var props)) return;

            var rect = target.GetComponent<RectTransform>();
            if (rect != null)
            {
                if (TryVector(props, "pos", out var pos)) rect.anchoredPosition = pos;
                if (TryVector(props, "size", out var size)) rect.sizeDelta = size;
                if (TryVector(props, "pivot", out var pivot)) rect.pivot = pivot;
                if (TryVector4(props, "anchors", out var a))
                {
                    rect.anchorMin = new Vector2(a.x, a.y);
                    rect.anchorMax = new Vector2(a.z, a.w);
                }
                if (TryVector(props, "scale", out var scale))
                    rect.localScale = new Vector3(scale.x, scale.y, 1f);
                if (TryFloat(props, "rotation", out var rot))
                    rect.localRotation = Quaternion.Euler(0f, 0f, rot);
                if (TryInt(props, "order", out var order))
                    rect.SetSiblingIndex(Mathf.Max(0, order));
            }

            var graphic = target.GetComponent<UnityEngine.UI.Graphic>();
            if (graphic != null)
            {
                if (TryColor(props, "tint", out var tint)) graphic.color = tint;
                if (TryFloat(props, "alpha", out var alpha))
                {
                    var c = graphic.color;
                    graphic.color = new Color(c.r, c.g, c.b, alpha);
                }
            }

            if (props.TryGetValue("text", out var textRaw) && textRaw is Dictionary<string, object?> textObj)
            {
                // The wheels use UnityEngine.UI.Text, but handle TMP too so a
                // later change of component type cannot silently drop styling.
                var label = target.GetComponent<UnityEngine.UI.Text>();
                if (label != null)
                {
                    if (TryInt(textObj, "size", out var fs) && fs > 0) label.fontSize = fs;
                    if (TryColor(textObj, "color", out var tc)) label.color = tc;
                    if (allowTextValue && textObj.TryGetValue("value", out var v) && v is string s)
                        label.text = s;
                }

                var tmp = target.GetComponent<TMPro.TMP_Text>();
                if (tmp != null)
                {
                    if (TryFloat(textObj, "size", out var tfs) && tfs > 0f) tmp.fontSize = tfs;
                    if (TryColor(textObj, "color", out var tcol)) tmp.color = tcol;
                    if (allowTextValue && textObj.TryGetValue("value", out var tv) && tv is string ts)
                        tmp.text = ts;
                }
            }

            if (props.TryGetValue("enabled", out var en) && en is bool enabled)
                target.SetActive(enabled);
        }

        /// <summary>
        /// What this profile would write for a node, as plain numbers.
        ///
        /// <see cref="Apply"/> needs live Unity components, which cannot be
        /// created outside the engine, so this exposes the same decisions as
        /// testable data: the parse, the key lookup and the guards are shared
        /// with Apply, only the final assignment differs.
        /// </summary>
        public Resolved Resolve(string nodeId, bool allowTextValue = true)
        {
            var result = new Resolved();
            if (_overrides == null) return result;
            if (!_overrides.TryGetValue(nodeId, out var props)) return result;

            result.Found = true;
            if (TryVector(props, "pos", out var pos)) result.Pos = new[] { pos.x, pos.y };
            if (TryVector(props, "size", out var size)) result.Size = new[] { size.x, size.y };
            if (TryVector(props, "scale", out var scale)) result.Scale = new[] { scale.x, scale.y };
            if (TryColor(props, "tint", out var tint))
                result.Tint = new[] { tint.r, tint.g, tint.b, tint.a };
            if (TryFloat(props, "alpha", out var alpha)) result.Alpha = alpha;
            if (TryFloat(props, "rotation", out var rot)) result.Rotation = rot;
            if (TryInt(props, "order", out var order)) result.Order = order;
            if (props.TryGetValue("enabled", out var en) && en is bool enabled) result.Enabled = enabled;

            if (props.TryGetValue("text", out var textRaw) && textRaw is Dictionary<string, object?> textObj)
            {
                if (TryFloat(textObj, "size", out var fs) && fs > 0f) result.TextSize = fs;
                if (TryColor(textObj, "color", out var tc))
                    result.TextColor = new[] { tc.r, tc.g, tc.b, tc.a };
                // Mirrors Apply: a game-owned string is never replaced.
                if (allowTextValue && textObj.TryGetValue("value", out var v) && v is string sv)
                    result.TextValue = sv;
            }
            return result;
        }

        /// <summary>Plain-data view of one node's overrides; see <see cref="Resolve"/>.</summary>
        public sealed class Resolved
        {
            public bool Found;
            public float[]? Pos;
            public float[]? Size;
            public float[]? Scale;
            public float[]? Tint;
            public float? Alpha;
            public float? Rotation;
            public int? Order;
            public bool? Enabled;
            public float? TextSize;
            public float[]? TextColor;
            public string? TextValue;
        }

        // ---------------------------------------------------------- parsing

        private static bool TryVector(Dictionary<string, object?> props, string key, out Vector2 value)
        {
            value = default;
            if (!props.TryGetValue(key, out var raw) || !(raw is List<object?> list) || list.Count < 2) return false;
            value = new Vector2(ToFloat(list[0], 0f), ToFloat(list[1], 0f));
            return true;
        }

        private static bool TryVector4(Dictionary<string, object?> props, string key, out Vector4 value)
        {
            value = default;
            if (!props.TryGetValue(key, out var raw) || !(raw is List<object?> list) || list.Count < 4) return false;
            value = new Vector4(ToFloat(list[0], 0f), ToFloat(list[1], 0f),
                                ToFloat(list[2], 0f), ToFloat(list[3], 0f));
            return true;
        }

        private static bool TryColor(Dictionary<string, object?> props, string key, out Color value)
        {
            value = default;
            if (!props.TryGetValue(key, out var raw) || !(raw is List<object?> list) || list.Count < 4) return false;
            value = new Color(ToFloat(list[0], 1f), ToFloat(list[1], 1f),
                              ToFloat(list[2], 1f), ToFloat(list[3], 1f));
            return true;
        }

        private static bool TryFloat(Dictionary<string, object?> props, string key, out float value)
        {
            value = 0f;
            if (!props.TryGetValue(key, out var raw)) return false;
            value = ToFloat(raw, 0f);
            return true;
        }

        private static bool TryInt(Dictionary<string, object?> props, string key, out int value)
        {
            value = 0;
            if (!props.TryGetValue(key, out var raw)) return false;
            value = ToInt(raw, 0);
            return true;
        }

        private static float ToFloat(object? raw, float fallback)
        {
            if (raw is double d) return (float)d;
            if (raw is long l) return l;
            if (raw is string s && float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var p))
                return p;
            return fallback;
        }

        private static int ToInt(object? raw, int fallback)
        {
            if (raw is long l) return (int)l;
            if (raw is double d) return (int)d;
            return fallback;
        }
    }
}
