using ItemChanger.Extensions;
using Sein.IC;
using Sein.Util;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Sein.Hud;

internal class SpiritLightFrameParticleFactory : UIParticleFactory<SpiritLightFrameParticleFactory, SpiritLightFrameParticle>
{
    private static readonly EmbeddedSprite sprite = new("SpiritLightFrameParticle");

    protected override string GetObjectName() => "SpiritLightFrameParticle";

    protected override Sprite GetSprite() => sprite.Value;
    public void Launch(float prewarm, Transform parent, float angle)
    {
        if (!Launch(prewarm, SpiritLightFrameParticle.FLIGHT_TIME, out var particle)) return;

        particle.SetParams(angle);
        particle.gameObject.transform.parent = parent;
    }
}

internal class SpiritLightFrameParticle : AbstractParticle<SpiritLightFrameParticleFactory, SpiritLightFrameParticle>
{
    private const float FLIGHT_DISTANCE = 1.35f;
    private const float FLIGHT_SPEED = 0.45f;
    internal const float FLIGHT_TIME = FLIGHT_DISTANCE / FLIGHT_SPEED;
    private const float SPAWN_RADIUS = 1.2f;

    private float angle;

    internal void SetParams(float angle) => this.angle = angle;

    protected override bool UseLocalPos => true;

    protected override Vector3 GetPos()
    {
        Vector3 fwd = new(SPAWN_RADIUS * FLIGHT_SPEED * Age, 0, 0);
        return Quaternion.Euler(0, 0, angle) * fwd;
    }

    protected override Vector3 GetScale()
    {
        float scale = Mathf.Sqrt(RProgress);
        return new(scale, scale, 1);
    }

    protected override float GetAlpha() => Mathf.Sqrt(RProgress);

    protected override SpiritLightFrameParticle Self() => this;
}

internal class SpiritLightParticleFactory : UIParticleFactory<SpiritLightParticleFactory, SpiritLightParticle>
{
    private static readonly EmbeddedSprite sprite = new("SpiritLightParticle");

    protected override string GetObjectName() => "SpiritLightParticle";

    protected override Sprite GetSprite() => sprite.Value;

    protected override int SortingOrder => 1;

    public void Launch(float prewarm, Transform parent, float angle, float scaleMult)
    {
        if (!Launch(prewarm, SpiritLightParticle.FLIGHT_TIME, out var particle)) return;

        particle.gameObject.transform.parent = parent;
        particle.SetParams(angle, scaleMult);
    }
}

internal class SpiritLightParticle : AbstractParticle<SpiritLightParticleFactory, SpiritLightParticle>
{
    private const float SCALE_BASE = 1f;
    private const float FLIGHT_DISTANCE = 1.1f;
    private const float FLIGHT_SPEED = 0.225f;
    internal const float FLIGHT_TIME = FLIGHT_DISTANCE / FLIGHT_SPEED;
    private const float SPAWN_RADIUS = 0.85f;

    private float angle;
    private float scaleMult;

    internal void SetParams(float angle, float scaleMult)
    {
        this.angle = angle;
        this.scaleMult = scaleMult;
    }

    protected override bool UseLocalPos => true;

    protected override Vector3 GetPos()
    {
        Vector3 fwd = new(scaleMult * (SPAWN_RADIUS + FLIGHT_SPEED * Age), 0, 0);
        return Quaternion.Euler(0, 0, angle) * fwd;
    }

    protected override Vector3 GetScale()
    {
        var scale = scaleMult * SCALE_BASE * Mathf.Sqrt(RProgress);
        return new(scale, scale, 1);
    }

    protected override float GetAlpha() => Mathf.Sqrt(RProgress);

    protected override SpiritLightParticle Self() => this;
}

internal class SpiritParticleUpdater
{
    private const int SPINDLES = 5;
    private const float REVOLUTION_TIME = 14;
    private const float ROT_SPEED = 360f / REVOLUTION_TIME;
    private const float PARTICLES_PER_SECOND = 4.5f;
    private const int MIN_TICKS = 110;
    private const int MAX_TICKS = 140;
    private static readonly int TICKS_PER_REVOLUTION = Mathf.FloorToInt(REVOLUTION_TIME * PARTICLES_PER_SECOND * (MIN_TICKS + MAX_TICKS) / 2);

    private readonly SpiritLightParticleFactory spiritLightParticleFactory = new();
    private readonly Transform parent;
    private readonly List<PeriodicFloatTicker> rotaryTickers = new();

    internal SpiritParticleUpdater(Transform parent)
    {
        this.parent = parent;

        rotaryTickers = new();
        for (int i = 0; i < SPINDLES; i++) rotaryTickers.Add(new(REVOLUTION_TIME, TICKS_PER_REVOLUTION, MIN_TICKS, MAX_TICKS));
    }

    private float rotation = 0;

    public void Update(float time, float scaleMult)
    {
        for (int i = 0; i < rotaryTickers.Count; i++)
        {
            float rotBase = rotation + i * 360f / rotaryTickers.Count;
            foreach (var used in rotaryTickers[i].TickFloats(time))
            {
                float angle = rotBase + used * ROT_SPEED;
                spiritLightParticleFactory.Launch(used, parent, angle, scaleMult);
            }
        }

        rotation += time * ROT_SPEED;
        while (rotation >= 360) rotation -= 360;
    }
}

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

        spiritParticleUpdater = new(spriteContainer.transform);

        On.GeoCounter.Update += UpdateGeoCounterOverride;
    }

    protected void OnDestroy()
    {
        On.GeoCounter.Update -= UpdateGeoCounterOverride;
    }

    private const int MAX_GEO = 20000;
    private const int MIN_GEO = 10;
    private static float MIN_SCALE = 0.05f;
    private static float SCALE_POW = 0.9f;
    private static float ROT_SPEED = 30;

    private float GetGeoScale(int counter)
    {
        if (counter >= MAX_GEO) return 1;
        else if (counter <= MIN_GEO + 1) return MIN_SCALE;

        float log = Mathf.Log(counter - MIN_GEO) / Mathf.Log(MAX_GEO - MIN_GEO);
        float p = Mathf.Pow(log, SCALE_POW);
        return MIN_SCALE + p * (1 - MIN_SCALE);
    }

    private float scale = MIN_SCALE;

    private void UpdateGeoCounter()
    {
        var currentGeo = (int)geoFieldInfo.GetValue(geoCounter);
        scale = GetGeoScale(currentGeo);
        light.transform.localScale = new(scale, scale, 1);
    }

    private void UpdateGeoCounterOverride(On.GeoCounter.orig_Update orig, GeoCounter self)
    {
        UpdateGeoCounter();
        orig(self);
    }

    private GameObject AddSprite(string name, Sprite sprite, int sortOrder)
    {
        var (obj, _) = UISprites.CreateUISprite(name, sprite, sortOrder);

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

    private SpiritParticleUpdater spiritParticleUpdater;

    private static readonly FieldInfo geoFieldInfo = typeof(GeoCounter).GetField("counterCurrent", BindingFlags.NonPublic | BindingFlags.Instance);

    protected void Update()
    {
        spiritLightText.text = realGeoText.text;
        spiritLightAddText.text = realGeoAddText.text;
        spiritLightSubtractText.text = realGeoSubtractText.text;

        spiritParticleUpdater.Update(Time.deltaTime, scale);
    }
}
