using GlobalEnums;
using ItemChanger.Extensions;
using Sein.IC;
using Sein.Util;
using System.Reflection;
using UnityEngine;

namespace Sein.Hud;

internal class SpiritLightHud : MonoBehaviour
{
    private static readonly EmbeddedSprite hudSprite = new("SpiritLightHud");
    private static readonly EmbeddedSprite lightSprite = new("SpiritLightOrb");

    private GeoCounter geoCounter;
    private TextMesh realGeoText;
    private TextMesh realGeoAddText;
    private TextMesh realGeoSubtractText;

    private GameObject spriteContainer;
    private GameObject container;
    private GameObject light;
    private TextMesh spiritLightText;
    private TextMesh spiritLightAddText;
    private TextMesh spiritLightSubtractText;

    protected void Awake()
    {
        var geoCounterObj = GOFinder.HudCanvas().FindChild("Geo Counter");
        geoCounter = geoCounterObj.GetComponent<GeoCounter>();
        realGeoText = geoCounterObj.FindChild("Geo Text").GetComponent<TextMesh>();
        realGeoAddText = geoCounterObj.FindChild("Add Text").GetComponent<TextMesh>();
        realGeoSubtractText = geoCounterObj.FindChild("Subtract Text").GetComponent<TextMesh>();

        spriteContainer = new("SpriteContainer");
        spriteContainer.transform.SetParent(transform);
        spriteContainer.transform.localPosition = Vector3.zero;
        spriteContainer.transform.localScale = new(0.71f, 0.71f, 0);
        container = AddSprite("Container", hudSprite.Value, 0);
        light = AddSprite("Light", lightSprite.Value, 1);

        spiritLightText = CloneTextMesh("Counter", realGeoText, new(0, -2.1f, 0));
        spiritLightAddText = CloneTextMesh("Adder", realGeoAddText, new(0, -2.8f, 0));
        spiritLightSubtractText = CloneTextMesh("Subtractor", realGeoSubtractText, new(0, -2.8f, 0));

        On.GeoCounter.Update += UpdateGeoCounterOverride;
    }

    protected void OnDestroy()
    {
        On.GeoCounter.Update -= UpdateGeoCounterOverride;
    }

    private const int MAX_GEO = 20000;
    private static float MIN_SCALE = 0.05f;
    private static float SCALE_POW = 0.9f;
    private static float ROT_SPEED = 30;

    private float GetGeoScale(int counter)
    {
        if (counter >= MAX_GEO) return 1;
        else if (counter == 0) return 0.01f;
        else if (counter == 1) return MIN_SCALE;

        float log = Mathf.Log(counter) / Mathf.Log(MAX_GEO);
        float p = Mathf.Pow(log, SCALE_POW);
        return MIN_SCALE + p * (1 - MIN_SCALE);
    }

    private void UpdateGeoCounter()
    {
        var currentGeo = (int)geoFieldInfo.GetValue(geoCounter);
        float scale = GetGeoScale(currentGeo);
        light.transform.localScale = new(scale, scale, 1);
    }

    private void UpdateGeoCounterOverride(On.GeoCounter.orig_Update orig, GeoCounter self)
    {
        UpdateGeoCounter();
        orig(self);
    }

    private GameObject AddSprite(string name, Sprite sprite, int sortOrder)
    {
        GameObject obj = new(name);
        obj.layer = (int)PhysLayers.UI;
        var renderer = obj.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerName = "Over";
        renderer.sortingOrder = sortOrder;
        obj.transform.SetParent(spriteContainer.transform);
        obj.transform.localScale = new(1, 1, 1);
        obj.transform.localPosition = Vector3.zero;
        return obj;
    }

    private TextMesh CloneTextMesh(string name, TextMesh prefab, Vector3 offset)
    {
        GameObject obj = Instantiate(prefab.gameObject);
        foreach (var fsm in obj.GetComponents<PlayMakerFSM>()) Destroy(fsm);
        obj.transform.SetParent(transform);
        obj.transform.localPosition = offset;

        obj.GetComponent<MeshRenderer>().sortingOrder = 2;

        var text = obj.GetComponent<TextMesh>();
        text.alignment = TextAlignment.Center;
        text.anchor = TextAnchor.MiddleCenter;
        text.fontSize = 36;

        return text;
    }

    private float angle = 0;

    private static readonly FieldInfo geoFieldInfo = typeof(GeoCounter).GetField("counterCurrent", BindingFlags.NonPublic | BindingFlags.Instance);
    private int currentGeo;

    protected void Update()
    {
        spiritLightText.text = realGeoText.text;
        spiritLightAddText.text = realGeoAddText.text;
        spiritLightSubtractText.text = realGeoSubtractText.text;

        angle = (angle + ROT_SPEED * Time.deltaTime) % 360;
        light.transform.localRotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }
}
