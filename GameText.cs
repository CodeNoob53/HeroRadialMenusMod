using HarmonyLib;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace HeroRadialMenusMod
{
    /// <summary>
    /// The few labels the wheels draw themselves; item names and tooltips come
    /// from the game's own localization.
    ///
    /// English and Ukrainian have their own wording here. The game has no keys
    /// for exactly these labels, so every other language borrows the closest
    /// ones it does have (the ballista's "no ammunition", the empty chest, the
    /// active ward) — translated into all of the game's languages, if worded
    /// for another context.
    ///
    /// Resolved on every call rather than registered with Localization.AddWord:
    /// the game reloads its dictionary when the language changes, which would
    /// silently drop registered words.
    /// </summary>
    internal static class GameText
    {
        public static string NoConsumables => Text(
            "No consumables", "Немає хілок",
            () => Localize("$radial_consumables") + ": " + Localize("$piece_container_empty"));

        public static string NoAmmo => Text(
            "No ammunition", "Немає стріл",
            () => Localize("$piece_turret_noammo"));

        public static string ActiveTag => Text(
            " (Active)", " (Активна)",
            () => " (" + Localize("$piece_guardstone_active") + ")");

        private static string Text(string en, string uk, System.Func<string> fromGame)
        {
            var loc = Localization.instance;
            string lang = loc != null ? loc.GetSelectedLanguage() : "English";
            if (lang == "Ukrainian") return uk;
            if (lang == "English" || loc == null) return en;

            // A missing key comes back as "[key]"; fall back to English then.
            string text = fromGame();
            return text.Contains("[") ? en : text;
        }

        private static string Localize(string key) => Localization.instance.Localize(key);
    }

    /// <summary>
    /// Whether a key already does something in the game's own controls, as the
    /// player has them bound right now (Settings → Controls). The arrow wheel
    /// only needs its hold delay on such a key, so a short press keeps its
    /// game action; on a free key the wheel can open at once.
    ///
    /// ZInput keeps its bindings private: the button table and the
    /// KeyCode → input-path conversion are reached by name. If a game update
    /// renames them, every key is treated as bound — the safe side, since it
    /// only brings the delay back.
    /// </summary>
    internal static class GameKeyBindings
    {
        private static readonly FieldInfo? ButtonsField =
            AccessTools.Field(typeof(ZInput), "m_buttons");
        private static readonly MethodInfo? KeyCodeToPathMethod =
            AccessTools.Method(typeof(ZInput), "KeyCodeToPath", new[] { typeof(KeyCode), typeof(bool) });
        private static bool _failureLogged;

        // ZInput registers every mouse button, Tab and Left Shift a second time
        // under a raw name so game code can ask "is mouse button N down". Those
        // entries are not actions: the real ones (Attack, Block, Inventory,
        // Run…) have their own entries. Ignoring the raw names keeps a free
        // side button from looking bound to "MouseForward".
        private static readonly System.Collections.Generic.HashSet<string> RawAliases =
            new System.Collections.Generic.HashSet<string>
            {
                "MouseLeft", "MouseRight", "MouseMiddle", "MouseForward", "MouseBack",
                "Tab", "LShift",
            };

        public static bool HasGameAction(KeyCode key)
        {
            if (key == KeyCode.None) return false;
            try
            {
                var input = ZInput.instance;
                if (input == null || ButtonsField == null || KeyCodeToPathMethod == null)
                    return LogFailure("ZInput недоступний");

                // Same Mouse3/Mouse4 numbering as the key reads; see Plugin.ToZInputKey.
                var path = KeyCodeToPathMethod.Invoke(null, new object[] { Plugin.ToZInputKey(key), false }) as string;
                if (string.IsNullOrEmpty(path)) return LogFailure($"немає шляху для {key}");

                if (!(ButtonsField.GetValue(input) is IDictionary buttons))
                    return LogFailure("таблиця кнопок недоступна");

                foreach (var value in buttons.Values)
                {
                    if (!(value is ZInput.ButtonDef def)) continue;
                    if (RawAliases.Contains(def.Name)) continue;
                    string? bound;
                    try { bound = def.GetActionPath(); }
                    catch { continue; } // an action without bindings
                    if (string.Equals(bound, path, System.StringComparison.OrdinalIgnoreCase))
                    {
                        Plugin.LogDebug($"GameKeyBindings: {key} ({path}) зайнята дією гри '{def.Name}'.");
                        return true;
                    }
                }
                return false;
            }
            catch (System.Exception ex)
            {
                return LogFailure(ex.Message);
            }
        }

        private static bool LogFailure(string reason)
        {
            if (!_failureLogged)
            {
                _failureLogged = true;
                Plugin.Log.LogWarning($"Не вдалося прочитати прив'язки клавіш гри ({reason}) — " +
                    "затримка HoldDelay діятиме для всіх клавіш колеса стріл.");
            }
            return true;
        }
    }
}
