using GlobalEnums;
using UnityEngine;

namespace Sein.Util;

internal static class UISprites
{
    public static (GameObject, SpriteRenderer) CreateUISprite(string name, Sprite sprite, int sortingOrder = 0)
    {
        GameObject obj = new(name);
        obj.layer = (int)PhysLayers.UI;

        var spriteRenderer = obj.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.sortingLayerName = "Over";
        spriteRenderer.sortingOrder = sortingOrder;

        return (obj, spriteRenderer);
    }
}
