using System;

namespace HeroRadialMenusMod
{
    /// <summary>Clockwise selection from the wheel-local mouse position; empty sectors never select.</summary>
    internal static class RadialSelection
    {
        public static int HoverIndex(float x, float y, float deadZone, int segments, int itemCount)
        {
            if (segments <= 0 || itemCount <= 0 || (x == 0f && y == 0f)) return -1;
            if (x * x + y * y < Math.Max(0f, deadZone) * Math.Max(0f, deadZone)) return -1;
            double angle = (Math.Atan2(x, y) * 180.0 / Math.PI + 360.0) % 360.0;
            int index = (int)Math.Round(angle / (360.0 / segments), MidpointRounding.AwayFromZero) % segments;
            return index < itemCount ? index : -1;
        }
    }
}
