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
}
