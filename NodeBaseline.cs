using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HeroRadialMenusMod
{
    /// <summary>
    /// Remembers what a node looked like before a profile ever touched it, so an
    /// override can actually be undone.
    ///
    /// Applying a profile only writes the keys it names. Without a baseline,
    /// dropping a key from the profile — or deleting the profile outright —
    /// left the old value sitting on a GameObject that had already been built:
    /// "reset" moved nothing back. The rule is therefore restore-then-apply,
    /// every time.
    ///
    /// Only the fields a profile can write are captured. Anything the game owns
    /// at runtime (item icons, counts, live text) is never recorded and never
    /// restored, so resetting the layout cannot clobber game state.
    /// </summary>
    internal sealed class NodeBaseline
    {
        private readonly Dictionary<string, Snapshot> _snapshots = new Dictionary<string, Snapshot>();

        private struct Snapshot
        {
            public bool HasRect;
            public Vector2 AnchoredPosition;
            public Vector2 SizeDelta;
            public Vector2 Pivot;
            public Vector2 AnchorMin;
            public Vector2 AnchorMax;
            public Vector3 LocalScale;
            public Quaternion LocalRotation;
            public int SiblingIndex;

            public bool HasGraphic;
            public Color Color;

            public bool HasText;
            public int FontSize;

            public bool ActiveSelf;
        }

        /// <summary>
        /// Record this node's current state, once. Called before the first
        /// profile application, so what is captured is base layout + .cfg —
        /// exactly what "no profile" should look like.
        /// </summary>
        public void Capture(string nodeId, GameObject? target)
        {
            if (target == null || _snapshots.ContainsKey(nodeId)) return;

            var snap = new Snapshot { ActiveSelf = target.activeSelf };

            var rect = target.GetComponent<RectTransform>();
            if (rect != null)
            {
                snap.HasRect = true;
                snap.AnchoredPosition = rect.anchoredPosition;
                snap.SizeDelta = rect.sizeDelta;
                snap.Pivot = rect.pivot;
                snap.AnchorMin = rect.anchorMin;
                snap.AnchorMax = rect.anchorMax;
                snap.LocalScale = rect.localScale;
                snap.LocalRotation = rect.localRotation;
                snap.SiblingIndex = rect.GetSiblingIndex();
            }

            var graphic = target.GetComponent<Graphic>();
            if (graphic != null)
            {
                snap.HasGraphic = true;
                snap.Color = graphic.color;
            }

            var label = target.GetComponent<Text>();
            if (label != null)
            {
                snap.HasText = true;
                snap.FontSize = label.fontSize;
            }

            _snapshots[nodeId] = snap;
        }

        /// <summary>
        /// Put the node back to its captured state. Safe to call when nothing
        /// was captured: it simply does nothing.
        /// </summary>
        public void Restore(string nodeId, GameObject? target)
        {
            if (target == null) return;
            if (!_snapshots.TryGetValue(nodeId, out var snap)) return;

            var rect = target.GetComponent<RectTransform>();
            if (snap.HasRect && rect != null)
            {
                rect.anchoredPosition = snap.AnchoredPosition;
                rect.sizeDelta = snap.SizeDelta;
                rect.pivot = snap.Pivot;
                rect.anchorMin = snap.AnchorMin;
                rect.anchorMax = snap.AnchorMax;
                rect.localScale = snap.LocalScale;
                rect.localRotation = snap.LocalRotation;
                if (rect.GetSiblingIndex() != snap.SiblingIndex)
                    rect.SetSiblingIndex(snap.SiblingIndex);
            }

            var graphic = target.GetComponent<Graphic>();
            if (snap.HasGraphic && graphic != null) graphic.color = snap.Color;

            var label = target.GetComponent<Text>();
            if (snap.HasText && label != null) label.fontSize = snap.FontSize;

            if (target.activeSelf != snap.ActiveSelf) target.SetActive(snap.ActiveSelf);
        }

        /// <summary>Forget everything; used when the UI is rebuilt from scratch.</summary>
        public void Clear() => _snapshots.Clear();

    }
}
