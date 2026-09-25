using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace HeroRadialMenusMod
{
    /// <summary>
    /// Quick-heal and arrow radial menus, split out of HeroEquipmentMod so the
    /// two can be installed independently.
    ///
    /// This assembly owns the HUD wheels and every input patch they need; the
    /// equipment panel stays in HeroEquipmentMod and is not referenced here.
    /// </summary>
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid    = "com.heromod.valheim.radialmenus";
        public const string PluginName    = "Hero Radial Menus";
        public const string PluginVersion = "1.1.1";

        internal static BepInEx.Logging.ManualLogSource Log { get; private set; } = null!;

        // --- Quick-heal wheel -------------------------------------------------
        public static ConfigEntry<KeyCode> RadialKey { get; private set; } = null!;
        public static ConfigEntry<bool>  RadialIncludeAllConsumables { get; private set; } = null!;
        public static ConfigEntry<float> RadialRadius { get; private set; } = null!;
        public static ConfigEntry<float> RadialSlotSize { get; private set; } = null!;
        public static ConfigEntry<float> RadialDeadZone { get; private set; } = null!;
        public static ConfigEntry<bool>  RadialSlowMotion { get; private set; } = null!;
        public static ConfigEntry<float> RadialEmptyAlpha { get; private set; } = null!;
        public static ConfigEntry<float> RadialHoverColorR { get; private set; } = null!;
        public static ConfigEntry<float> RadialHoverColorG { get; private set; } = null!;
        public static ConfigEntry<float> RadialHoverColorB { get; private set; } = null!;
        public static ConfigEntry<float> RadialHoverAlpha { get; private set; } = null!;
        public static ConfigEntry<float> RadialHoverSpeed { get; private set; } = null!;
        public static ConfigEntry<float> RadialOrnamentOffset { get; private set; } = null!;
        public static ConfigEntry<bool>  RadialShowFrame { get; private set; } = null!;
        public static ConfigEntry<bool>  RadialTintBackground { get; private set; } = null!;
        public static ConfigEntry<float> RadialFrameColorR { get; private set; } = null!;
        public static ConfigEntry<float> RadialFrameColorG { get; private set; } = null!;
        public static ConfigEntry<float> RadialFrameColorB { get; private set; } = null!;
        public static ConfigEntry<float> RadialFrameAlpha { get; private set; } = null!;
        public static ConfigEntry<float> RadialFrameWidth { get; private set; } = null!;
        public static ConfigEntry<bool>  RadialBlockMovement { get; private set; } = null!;
        public static ConfigEntry<bool>    RadialClickToUse { get; private set; } = null!;
        public static ConfigEntry<KeyCode> RadialClickKey { get; private set; } = null!;
        public static ConfigEntry<bool>    RadialConsumeAnimation { get; private set; } = null!;

        // --- Arrow wheel ------------------------------------------------------
        public static ConfigEntry<KeyCode> ArrowRadialKey { get; private set; } = null!;
        public static ConfigEntry<KeyCode> ArrowRadialKeySecondary { get; private set; } = null!;
        public static ConfigEntry<float>   ArrowRadialHoldDelay { get; private set; } = null!;
        public static ConfigEntry<float>   ArrowRadialPickDelay { get; private set; } = null!;
        public static ConfigEntry<float>   ArrowRadialOffsetX { get; private set; } = null!;
        public static ConfigEntry<float>   ArrowRadialOffsetY { get; private set; } = null!;

        // Position of the heal wheel. Kept in [Position] under the same key
        // names the old config used, so migration is a straight copy.
        public static ConfigEntry<float> HealOffsetX { get; private set; } = null!;
        public static ConfigEntry<float> HealOffsetY { get; private set; } = null!;

        public static ConfigEntry<bool> DebugLogs { get; private set; } = null!;

        private readonly Harmony _harmony = new Harmony(PluginGuid);

        public static Color RadialHoverColor
        {
            get
            {
                float r = RadialHoverColorR.Value;
                float g = RadialHoverColorG.Value;
                float b = RadialHoverColorB.Value;
                float a = RadialHoverAlpha.Value;
                return new Color(r, g, b, Mathf.Clamp01(a));
            }
        }

        public static Color RadialFrameColor => new Color(
            RadialFrameColorR.Value,
            RadialFrameColorG.Value,
            RadialFrameColorB.Value,
            Mathf.Clamp01(RadialFrameAlpha.Value));

        private void Awake()
        {
            Log = Logger;

            BindConfig();
            WatchConfigFile();

            _harmony.PatchAll();
            Logger.LogInfo($"{PluginName} {PluginVersion} завантажено.");
        }

        private void OnDestroy()
        {
            // Leaving patches behind after an unload would keep the player's
            // controls blocked, so drop them and close any open wheel first.
            try { QuickHealRadial.ForceClose(); } catch { }
            try { ArrowRadial.ForceClose(); } catch { }
            _cfgWatcher?.Dispose();
            _cfgWatcher = null;
            _harmony.UnpatchSelf();
        }

        // ------------------------------------------------ live CFG reload

        // Edits to the .cfg made while the game runs are picked up without a
        // restart. The watcher fires on a worker thread, so it only records the
        // time; the reload itself happens on the main thread in PollConfigReload.
        private static FileSystemWatcher? _cfgWatcher;
        private static ConfigFile? _cfg;
        private static long _cfgChangedTicks;

        // Editors often write a file in several steps; wait until it settles.
        private static readonly long CfgSettleTicks = TimeSpan.FromMilliseconds(300).Ticks;

        private void WatchConfigFile()
        {
            _cfg = Config;
            try
            {
                string path = Config.ConfigFilePath;
                _cfgWatcher = new FileSystemWatcher(Path.GetDirectoryName(path), Path.GetFileName(path))
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.FileName,
                };
                FileSystemEventHandler onChange = (_, __) =>
                    System.Threading.Interlocked.Exchange(ref _cfgChangedTicks, DateTime.UtcNow.Ticks);
                _cfgWatcher.Changed += onChange;
                _cfgWatcher.Created += onChange;
                _cfgWatcher.Renamed += (_, __) =>
                    System.Threading.Interlocked.Exchange(ref _cfgChangedTicks, DateTime.UtcNow.Ticks);
                _cfgWatcher.EnableRaisingEvents = true;
            }
            catch (Exception ex)
            {
                _cfgWatcher = null;
                Logger.LogWarning($"Не вдалося стежити за CFG, зміни діятимуть після перезапуску: {ex.Message}");
            }
        }

        /// <summary>Called every HUD frame; reloads the .cfg once it has settled.</summary>
        public static void PollConfigReload()
        {
            long changed = System.Threading.Interlocked.Read(ref _cfgChangedTicks);
            if (changed == 0 || _cfg == null) return;
            if (DateTime.UtcNow.Ticks - changed < CfgSettleTicks) return;
            System.Threading.Interlocked.CompareExchange(ref _cfgChangedTicks, 0, changed);

            // Reload fires SettingChanged, which would save the file straight
            // back and wake the watcher again.
            bool save = _cfg.SaveOnConfigSet;
            _cfg.SaveOnConfigSet = false;
            try
            {
                _cfg.Reload();
                Log.LogInfo("CFG перечитано з диска.");
            }
            catch (Exception ex)
            {
                // Usually the editor still holds the file; try again shortly.
                System.Threading.Interlocked.CompareExchange(ref _cfgChangedTicks, DateTime.UtcNow.Ticks, 0);
                LogDebug($"CFG ще не вдалося перечитати: {ex.Message}");
            }
            finally
            {
                _cfg.SaveOnConfigSet = save;
            }
        }

        private void BindConfig()
        {
            // Mouse0=LMB, Mouse1=RMB, Mouse2=wheel (taken by the game).
            // Mouse3 is a side button and is free in Valheim.
            RadialKey = Config.Bind("Radial", "RadialKey", KeyCode.Mouse3,
                "Основна клавіша радіального меню хілок (тримати). За замовчуванням Mouse3 (задня бокова кнопка миші)");
            RadialIncludeAllConsumables = Config.Bind("Radial", "IncludeAllConsumables", false,
                "true — усі консумабли; false — їжа та консумабли зі статус-ефектом (зілля, медовуха)");
            RadialRadius = Config.Bind("Radial", "Radius", 320f, "Радіус кола в пікселях");
            RadialSlotSize = Config.Bind("Radial", "SlotSize", 68f, "Розмір слота в пікселях");
            RadialDeadZone = Config.Bind("Radial", "DeadZone", 100f,
                "Мертва зона в центрі — в ній нічого не вибрано (відміна)");
            RadialHoverSpeed = Config.Bind("Radial", "HoverFadeSeconds", 0.12f,
                "За скільки секунд підсвітка плавно з'являється/зникає");
            RadialEmptyAlpha = Config.Bind("Radial", "EmptySlotAlpha", 0.35f,
                "Прозорість порожніх секторів (0..1). Менше = блідіше, " +
                "щоб відрізнялись від секторів із предметами");
            RadialHoverColorR = Config.Bind("Radial", "HoverColorR", 0.23f,
                "Червона складова фону секції під hover (0..1)");
            RadialHoverColorG = Config.Bind("Radial", "HoverColorG", 0.14f,
                "Зелена складова фону секції під hover (0..1)");
            RadialHoverColorB = Config.Bind("Radial", "HoverColorB", 0.03f,
                "Синя складова фону секції під hover (0..1)");
            RadialHoverAlpha = Config.Bind("Radial", "HoverAlpha", 0.45f,
                "Прозорість фону секції під hover (0..1)");
            RadialFrameColorR = Config.Bind("Radial", "FrameColorR", 1.0f,
                "Червона складова контурної рамки під hover (0..1)");
            RadialFrameColorG = Config.Bind("Radial", "FrameColorG", 0.72f,
                "Зелена складова контурної рамки під hover (0..1)");
            RadialFrameColorB = Config.Bind("Radial", "FrameColorB", 0.36f,
                "Синя складова контурної рамки під hover (0..1)");
            RadialFrameAlpha = Config.Bind("Radial", "FrameAlpha", 1.0f,
                "Прозорість контурної рамки під hover (0..1)");
            RadialFrameWidth = Config.Bind("Radial", "FrameWidth", 4f,
                "Товщина ліній контурної рамки у пікселях (1..8)");
            RadialOrnamentOffset = Config.Bind("Radial", "OrnamentOffset", 115f,
                "Відступ орнаменту від Radius у пікселях; зовнішній край сектора — Radius + 94. Наприклад, 109 дає зазор 15 пікселів");
            RadialSlowMotion = Config.Bind("Radial", "SlowMotion", false,
                "Експериментально: локальний час 0.25x, поки відкрите меню консумаблів. Для одиночної гри; не синхронізується із сервером");
            RadialShowFrame = Config.Bind("Radial", "ShowFrame", true,
                "Малювати тонку золоту контурну рамку навколо активного сектора");
            RadialTintBackground = Config.Bind("Radial", "TintBackground", false,
                "Заливати фон активного сектора кольором підсвітки (у ванілі фон чорний)");
            RadialBlockMovement = Config.Bind("Radial", "BlockMovement", false,
                "Повністю блокувати рух персонажа (WASD), поки відкрите радіальне меню (якщо false — дозволено ходити, як у ванільному інвентарі)");
            RadialClickToUse = Config.Bind("Radial", "ClickToUse", true,
                "Клік (ClickKey) по слоту одразу застосовує предмет, меню лишається відкритим — " +
                "можна випити кілька зіль за одне відкриття. Якщо за відкриття був хоч один клік, " +
                "відпускання клавіші меню просто закриває його, нічого не застосовуючи");
            RadialClickKey = Config.Bind("Radial", "ClickKey", KeyCode.Mouse0,
                "Клавіша застосування по кліку (ClickToUse). За замовчуванням Mouse0 (ЛКМ)");
            RadialConsumeAnimation = Config.Bind("Radial", "ConsumeAnimation", true,
                "Програвати ванільну анімацію вживання (пиття/їжі) при застосуванні з колеса. " +
                "false — предмет застосовується без анімації; звук і ефект вживання лишаються");

            // "R" is the vanilla Hide-weapon toggle. Short presses stay vanilla;
            // only a hold longer than HoldDelay opens the arrow wheel on top.
            ArrowRadialKey = Config.Bind("ArrowRadial", "Key", KeyCode.R,
                "Основна клавіша меню вибору стріли (тримати, поки лук у руках)");
            ArrowRadialKeySecondary = Config.Bind("ArrowRadial", "KeySecondary", KeyCode.None,
                "Додаткова/альтернативна клавіша меню вибору стріли (якщо не потрібна — None)");
            ArrowRadialHoldDelay = Config.Bind("ArrowRadial", "HoldDelay", 0.25f,
                "Скільки секунд тримати клавішу, перш ніж відкриється меню — " +
                "коротке натискання лишається ванільним 'сховати зброю'");
            ArrowRadialPickDelay = Config.Bind("ArrowRadial", "PickDelay", 0.15f,
                "Скільки секунд меню лишається на екрані після вибору стріли — " +
                "щоб побачити, що саме застосувалось");
            ArrowRadialOffsetX = Config.Bind("ArrowRadial", "OffsetX", 0f, "Зміщення кола по X");
            ArrowRadialOffsetY = Config.Bind("ArrowRadial", "OffsetY", 0f, "Зміщення кола по Y");

            HealOffsetX = Config.Bind("Position", "HealOffsetX", 0f, "Зміщення панелі хілу по X");
            HealOffsetY = Config.Bind("Position", "HealOffsetY", 0f, "Зміщення панелі хілу по Y");

            DebugLogs = Config.Bind("Debug", "EnableDebugLogs", false,
                "Писати в лог детальну діагностику UI, профілів і клавіш; однакові повідомлення вводу — один раз за сеанс");
        }

        public static void LogDebug(string message)
        {
            if (DebugLogs != null && DebugLogs.Value)
            {
                Log?.LogInfo(message);
            }
        }

        // ------------------------------------------------------------- input

        // Read input from both sources: ZInput (the new system the game itself
        // uses) and legacy UnityEngine.Input. Which one is live depends on the
        // backend the game build enables, so relying on either alone is fragile.
        private static readonly HashSet<string> _loggedInput = new HashSet<string>();

        private static void LogInputOnce(string what)
        {
            if (DebugLogs != null && DebugLogs.Value)
            {
                if (!_loggedInput.Add(what)) return;
                Log?.LogInfo($"[input] {what}");
            }
        }

        public static bool KeyHeld(KeyCode key)
        {
            bool legacy = false, zin = false;
            try { legacy = Input.GetKey(key); } catch { }
            try { zin = ZInput.GetKey(key, false); } catch { }

            if (legacy || zin)
                LogInputOnce($"KeyHeld({key}): Input={legacy}, ZInput={zin}");
            return legacy || zin;
        }

        public static bool KeyPressed(KeyCode key)
        {
            bool legacy = false, zin = false;
            try { legacy = Input.GetKeyDown(key); } catch { }
            try { zin = ZInput.GetKeyDown(key, false); } catch { }

            if (legacy || zin)
                LogInputOnce($"KeyPressed({key}): Input={legacy}, ZInput={zin}");
            return legacy || zin;
        }

        public static bool KeyReleased(KeyCode key)
        {
            bool legacy = false, zin = false;
            try { legacy = Input.GetKeyUp(key); } catch { }
            try { zin = ZInput.GetKeyUp(key, false); } catch { }

            if (legacy || zin)
                LogInputOnce($"KeyReleased({key}): Input={legacy}, ZInput={zin}");
            return legacy || zin;
        }

        public static bool CanUseHotkeys()
        {
            if (InventoryGui.IsVisible()) return false;
            if (Menu.IsVisible()) return false;
            if (Minimap.IsOpen()) return false;
            if (global::Console.IsVisible()) return false;
            if (Chat.instance != null && Chat.instance.HasFocus()) return false;
            var p = Player.m_localPlayer;
            if (p == null) return false;
            if (p.IsDead()) return false;
            if (p.IsTeleporting()) return false;
            return true;
        }
    }
}
