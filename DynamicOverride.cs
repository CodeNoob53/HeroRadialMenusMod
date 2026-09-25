using UnityEngine;

namespace HeroRadialMenusMod
{
    /// <summary>
    /// How a layer-profile override combines with live, game-owned state for the
    /// nodes the wheel's update loop rewrites every frame.
    ///
    /// The highlighter, the chevron cursor and the item slots are not static
    /// layout. Their colour comes from .cfg, their visibility from hover / aim /
    /// whether a slot holds an item, and their transform from the current angle.
    /// Writing a profile value onto them once — the way a static node is handled
    /// — lasts exactly one frame: the next update puts the .cfg colour back and
    /// re-enables what the profile hid.
    ///
    /// So the profile is resolved into plain values and merged here, at the
    /// point of update. The rules are deliberately small and identical for every
    /// such node:
    ///
    ///   visible = the game wants it visible AND the profile has not disabled it
    ///   colour  = the profile's tint if it set one, otherwise the .cfg colour
    ///   alpha   = that colour's alpha, times the animated fade, times the
    ///             profile's alpha
    ///
    /// Multiplying the alphas rather than letting one win keeps the hover fade
    /// working on an ornament the user dimmed: a profile alpha of 0.5 halves it
    /// throughout the animation instead of pinning it to a constant.
    ///
    /// Geometry is deliberately absent. These nodes are positioned by angle every
    /// frame, so a profile that pinned their position would freeze the animation.
    /// This is a plain static class with no Unity object access, so the rules are
    /// tested directly rather than inferred from source text.
    /// </summary>
    internal static class DynamicOverride
    {
        /// <summary>
        /// Whether a node the game wants shown should actually be shown.
        /// A profile can only hide, never force-show: if the game has nothing to
        /// display there is nothing to reveal.
        /// </summary>
        public static bool Visible(bool gameWantsVisible, bool profileEnabled) =>
            gameWantsVisible && profileEnabled;

        /// <summary>
        /// Final colour for a node whose base colour comes from .cfg and whose
        /// alpha is animated.
        /// </summary>
        /// <param name="cfgColor">Colour the mod would use with no profile.</param>
        /// <param name="profileTint">Tint from the profile, or null if unset.</param>
        /// <param name="animationAlpha">The wheel's own fade, 0..1.</param>
        /// <param name="profileAlpha">Alpha from the profile; 1 when unset.</param>
        public static Color Tint(Color cfgColor, Color? profileTint,
                                 float animationAlpha, float profileAlpha)
        {
            Color baseColor = profileTint ?? cfgColor;
            return new Color(
                baseColor.r,
                baseColor.g,
                baseColor.b,
                baseColor.a * animationAlpha * profileAlpha);
        }
    }
}
