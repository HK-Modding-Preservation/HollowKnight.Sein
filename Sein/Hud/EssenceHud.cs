using ItemChanger.Extensions;
using PurenailCore.GOUtil;
using Sein.Util;
using Sein.Watchers;
using UnityEngine;

namespace Sein.Hud;

internal class EssenceHud : MonoBehaviour
{
    private static readonly IC.EmbeddedSprite sprite = new("essence");

    private GameObject spriteObj;
    private SpriteRenderer spriteRenderer;
    private TextMesh textObj;

    protected void Awake()
    {
        var geoCounterObj = GOFinder.HudCanvas().FindChild("Geo Counter");
        var geoText = geoCounterObj.FindChild("Geo Text").GetComponent<TextMesh>();

        (spriteObj, spriteRenderer) = UISprites.CreateUISprite("SpriteObj", sprite.Value);
        spriteObj.transform.SetParent(transform);
        spriteObj.transform.localPosition = Vector3.zero;

        textObj = UISprites.CloneTextMesh("Essence", geoText, transform, new(0.55f, -0.1f, 0));
        textObj.alignment = TextAlignment.Left;
        textObj.anchor = TextAnchor.MiddleLeft;
        textObj.fontSize = 48;
    }

    private const float BASE_SCALE = 0.6f;
    private const float FINAL_SCALE = 1.4f;
    private const float FADE_IN = 0.4f;
    private const float FADE_OUT = 0.7f;

    private ProgressFloat progress = new(0, 1 / FADE_IN, 1 / FADE_OUT);

    private const float ROT_SPEED = 360f / 15f;
    private float angle = 0;

    protected void Update()
    {
        textObj.text = PlayerDataCache.Instance.EssenceText;

        progress.Advance(Time.deltaTime, EssenceWatcher.EssenceUp ? 1 : 0);
        textObj.color = textObj.color.WithAlpha(progress.Value);
        spriteRenderer.SetAlpha(progress.Value);

        var scale = BASE_SCALE + (FINAL_SCALE - BASE_SCALE) * progress.Value;
        spriteObj.transform.localScale = new(scale, scale, 1);

        angle += Time.deltaTime * ROT_SPEED;
        while (angle >= 360) angle -= 360;
        spriteObj.transform.localRotation = Quaternion.Euler(0, 0, angle);
    }
}
