using UnityEngine;

namespace Sein.Util;

internal static class GameObjectExtensions
{
    public static void SetAlpha(this SpriteRenderer self, float alpha)
    {
        var c = self.color;
        c.a = alpha;
        self.color = c;
    }

    private static float Interploate(float a, float f, float b) => a + (b - a) * f;

    public static Color Interpolate(this Color self, float f, Color other)
    {
        float r = Interploate(self.r, f, other.r);
        float g = Interploate(self.g, f, other.g);
        float b = Interploate(self.b, f, other.b);
        float a = Interploate(self.a, f, other.a);
        return new Color(r, g, b, a);
    }

    public static Color Darker(this Color self, float f)
    {
        var black = Color.black;
        black.a = self.a;
        return self.Interpolate(f, black);
    }
}
