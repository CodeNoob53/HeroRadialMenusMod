using HarmonyLib;
using UnityEngine;

namespace HeroRadialMenusMod
{
    // Mirror the game's radial cursor branch without changing Hud.InRadial(),
    // which also controls vanilla menu interaction elsewhere in the game.
    [HarmonyPatch(typeof(GameCamera), nameof(GameCamera.UpdateMouseCapture))]
    public static class RadialMouseCapturePatch
    {
        static bool Prefix()
        {
            if (!QuickHealRadial.IsOpen && !ArrowRadial.IsOpen) return true;
            ZCursor.LockState = ZInput.IsMouseActive() ? CursorLockMode.None : CursorLockMode.Locked;
            ZCursor.Show();
            return false;
        }
    }

    // =============================================
    // ConsumeAnimation = false: Humanoid.UseItem is the only place that fires
    // the "eat" trigger and puts the item in the hand. Both are skipped, but
    // only for the call the heal wheel makes — inventory and hotbar stay vanilla.
    // =============================================
    [HarmonyPatch(typeof(ZSyncAnimation), nameof(ZSyncAnimation.SetTrigger))]
    public static class SuppressEatAnimationPatch
    {
        static bool Prefix(string name) =>
            !(QuickHealRadial.SuppressEatAnimation && name == "eat");
    }

    [HarmonyPatch(typeof(Humanoid), "SetUseHandVisual")]
    public static class SuppressUseHandVisualPatch
    {
        static bool Prefix() => !QuickHealRadial.SuppressEatAnimation;
    }

    // =============================================
    // HUD — both wheels live on the HUD, so they are created and ticked here.
    // =============================================
    [HarmonyPatch(typeof(Hud), "Awake")]
    public static class HudAwakePatch
    {
        static void Postfix(Hud __instance)
        {
            QuickHealRadial.TryCreate(__instance);
            ArrowRadial.TryCreate(__instance);
        }
    }

    [HarmonyPatch(typeof(Hud), "Update")]
    public static class HudUpdatePatch
    {
        static void Postfix()
        {
            try { Plugin.PollConfigReload(); }
            catch (System.Exception ex) { Plugin.Log.LogError($"PollConfigReload error: {ex.Message}"); }

            // Each wheel is ticked in its own try so a failure in one cannot
            // leave the other stuck open with the controls blocked.
            try
            {
                QuickHealRadial.UpdateRadial();
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"QuickHealRadial.UpdateRadial error: {ex.Message}");
                try { QuickHealRadial.ForceClose(); } catch { }
            }

            try
            {
                ArrowRadial.UpdateRadial();
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"ArrowRadial.UpdateRadial error: {ex.Message}");
                try { ArrowRadial.ForceClose(); } catch { }
            }
        }
    }

    // =============================================
    // Blocking player control while a wheel is open, the same way vanilla
    // blocks it for the inventory (InInventoryEtc + TakeInput):
    // 1) PlayerController.InInventoryEtc -> blocks mouse-look (SetMouseLook(zero))
    //    and attack/block/jump/dodge in FixedUpdate
    // 2) Player.TakeInput -> disables interact (E), the hotkey bar (1-8),
    //    guardian power and hide-weapon
    // 3) PlayerController.TakeInput -> disables gamepad sticks (or all input
    //    when BlockMovement is on)
    // 4) Player.SetControls -> forces the action flags off
    // =============================================
    [HarmonyPatch(typeof(PlayerController), "InInventoryEtc")]
    public static class PlayerControllerInInventoryEtcPatch
    {
        static void Postfix(ref bool __result)
        {
            if (QuickHealRadial.IsOpen || ArrowRadial.IsOpen)
                __result = true;
        }
    }

    [HarmonyPatch(typeof(Player), "TakeInput")]
    public static class PlayerTakeInputPatch
    {
        static void Postfix(ref bool __result)
        {
            if (QuickHealRadial.IsOpen || ArrowRadial.IsOpen)
                __result = false;
        }
    }

    [HarmonyPatch(typeof(PlayerController), "TakeInput")]
    public static class PlayerControllerTakeInputPatch
    {
        static void Postfix(ref bool __result)
        {
            if (QuickHealRadial.IsOpen || ArrowRadial.IsOpen)
            {
                if (Plugin.RadialBlockMovement != null && Plugin.RadialBlockMovement.Value)
                    __result = false;
                else if (ZInput.IsGamepadActive())
                    __result = false;
            }
        }
    }

    [HarmonyPatch(typeof(Player), "SetControls")]
    public static class PlayerSetControlsPatch
    {
        static void Prefix(
            ref Vector3 movedir,
            ref bool attack,
            ref bool attackHold,
            ref bool secondaryAttack,
            ref bool secondaryAttackHold,
            ref bool block,
            ref bool blockHold,
            ref bool jump,
            ref bool crouch,
            ref bool run,
            ref bool autoRun,
            ref bool dodge)
        {
            if (QuickHealRadial.IsOpen || ArrowRadial.IsOpen)
            {
                if (Plugin.RadialBlockMovement != null && Plugin.RadialBlockMovement.Value)
                {
                    movedir = Vector3.zero;
                }
                attack = false;
                attackHold = false;
                secondaryAttack = false;
                secondaryAttackHold = false;
                block = false;
                blockHold = false;
                jump = false;
                crouch = false;
                run = false;
                autoRun = false;
                dodge = false;
            }
        }
    }

    // =============================================
    // Death and scene changes must not leave a wheel open: the open flag drives
    // the input patches above, so a stale one would lock the player out.
    // =============================================
    [HarmonyPatch(typeof(Player), "OnDeath")]
    public static class PlayerOnDeathPatch
    {
        static void Postfix()
        {
            QuickHealRadial.ForceClose();
            ArrowRadial.ForceClose();
        }
    }

    [HarmonyPatch(typeof(Game), "OnDestroy")]
    public static class GameOnDestroyPatch
    {
        static void Prefix()
        {
            QuickHealRadial.ForceClose();
            ArrowRadial.ForceClose();
        }
    }
}
