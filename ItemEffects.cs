using UnityEngine;

namespace HeroRadialMenusMod
{
    /// <summary>
    /// What a consumable restores — health, stamina, eitr — and in which
    /// proportion, so its slot can be coloured the way the HUD bars are.
    ///
    /// Food states it directly (m_food / m_foodStamina / m_foodEitr). Meads
    /// carry an SE_Stats: its up-front plus over-time amounts count, and a
    /// regeneration boost alone (Tasty mead and the like) counts as a small
    /// share so the mead still gets its colour. Resistance meads restore none
    /// of the three and stay uncoloured.
    /// </summary>
    internal readonly struct ItemEffects
    {
        public readonly float Health;
        public readonly float Stamina;
        public readonly float Eitr;

        private ItemEffects(float health, float stamina, float eitr)
        {
            Health = health;
            Stamina = stamina;
            Eitr = eitr;
        }

        public bool Any => Health > 0f || Stamina > 0f || Eitr > 0f;

        // Close to the vanilla HUD bars, a touch brighter so they read on the
        // dark sectors.
        public static readonly Color HealthColor  = new Color(0.90f, 0.20f, 0.16f);
        public static readonly Color StaminaColor = new Color(1.00f, 0.80f, 0.18f);
        public static readonly Color EitrColor    = new Color(0.45f, 0.42f, 1.00f);

        /// <summary>
        /// A stat within this share of the largest one counts as a main effect
        /// too. 33/33 jerky is split evenly; a 70/42 wrap is plain health.
        /// </summary>
        private const float BalancedShare = 0.75f;

        /// <summary>
        /// The colours to show, in bar order (health, stamina, eitr): the
        /// largest stat plus any close to it. One colour for most items; two or
        /// three for balanced food, drawn as a split gradient.
        /// </summary>
        public int GetMainColors(Color[] colors)
        {
            float max = Mathf.Max(Health, Mathf.Max(Stamina, Eitr));
            if (max <= 0f) return 0;
            float min = max * BalancedShare;
            int n = 0;
            if (Health >= min && n < colors.Length) colors[n++] = HealthColor;
            if (Stamina >= min && n < colors.Length) colors[n++] = StaminaColor;
            if (Eitr >= min && n < colors.Length) colors[n++] = EitrColor;
            return n;
        }

        public static ItemEffects Of(ItemDrop.ItemData? item)
        {
            var sh = item?.m_shared;
            if (sh == null || sh.m_itemType != ItemDrop.ItemData.ItemType.Consumable)
                return default;

            float health = Mathf.Max(0f, sh.m_food);
            float stamina = Mathf.Max(0f, sh.m_foodStamina);
            float eitr = Mathf.Max(0f, sh.m_foodEitr);

            if (sh.m_consumeStatusEffect is SE_Stats se)
            {
                health += Restored(se.m_healthUpFront, se.m_healthOverTime, se.m_healthRegenMultiplier);
                stamina += Restored(se.m_staminaUpFront, se.m_staminaOverTime, se.m_staminaRegenMultiplier);
                eitr += Restored(se.m_eitrUpFront, se.m_eitrOverTime, se.m_eitrRegenMultiplier);
            }
            return new ItemEffects(health, stamina, eitr);
        }

        private static float Restored(float upFront, float overTime, float regenMultiplier)
        {
            float amount = Mathf.Max(0f, upFront) + Mathf.Max(0f, overTime);
            if (amount <= 0f && regenMultiplier > 1f) amount = 1f;
            return amount;
        }
    }
}
