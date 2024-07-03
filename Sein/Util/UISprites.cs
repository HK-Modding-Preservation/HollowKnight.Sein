using GlobalEnums;
using UnityEngine;

namespace Sein.Util;

internal static class UISprites
{
    public static TextMesh CloneTextMesh(string name, TextMesh prefab, Transform parent, Vector3 offset)
    {
        GameObject obj = Object.Instantiate(prefab.gameObject);
        foreach (var fsm in obj.GetComponents<PlayMakerFSM>()) Object.Destroy(fsm);
        obj.transform.SetParent(parent);
        obj.transform.localPosition = offset;

        obj.GetComponent<MeshRenderer>().sortingOrder = 2;

        var text = obj.GetComponent<TextMesh>();
        text.alignment = TextAlignment.Center;
        text.anchor = TextAnchor.MiddleCenter;
        text.fontSize = 36;

        return text;
    }

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
