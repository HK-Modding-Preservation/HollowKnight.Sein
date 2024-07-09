using ItemChanger.Extensions;
using Sein.Util;
using UnityEngine;

namespace Sein.Effects;

internal class RegenerateParticleFactory : AbstractParticleFactory<RegenerateParticleFactory, RegenerateParticle>
{
    private const float LIFETIME = 0.45f;

    private static IC.EmbeddedSprite sprite = new("RegenerateParticle");

    protected override string GetObjectName() => "RegenerateParticle";

    protected override Sprite GetSprite() => sprite.Value;

    public void Launch(float prewarm, Vector3 dest)
    {
        float lifetime = LIFETIME;
        if (PlayerDataCache.Instance.QuickFocusEquipped) lifetime /= Regenerator.QUICK_FOCUS_SPEEDUP;
        if (PlayerDataCache.Instance.DeepFocusEquipped) lifetime *= Regenerator.DEEP_FOCUS_SLOWDOWN;
        if (!Launch(prewarm, lifetime, out var particle)) return;

        particle.SetParams(dest);
        particle.Finalize(prewarm);
    }
}

internal class RegenerateParticle : AbstractParticle<RegenerateParticleFactory, RegenerateParticle>
{
    private const float SCALE_BASE = 1.8f;
    private const float DEEP_FOCUS_SCALE_BASE = 2.9f;
    private const float REVOLUTIONS = 0.55f;
    private const float RADIUS_MIN = 3.5f;
    private const float RADIUS_MAX = 6f;
    private const float ALPHA_FADE = 0.8f;

    private float rotBase;
    private Vector3 center;
    private Vector3 radial;
    private float scaleBase;

    internal void SetParams(Vector3 center)
    {
        this.center = center;
        this.rotBase = Random.Range(0, 360f);

        var radius = Random.Range(RADIUS_MIN, RADIUS_MAX);
        var angle = Random.Range(0, 360f);
        this.radial = center + Quaternion.Euler(0, 0, angle) * new Vector3(radius, 0, 0);
        this.scaleBase = PlayerDataCache.Instance.DeepFocusEquipped ? DEEP_FOCUS_SCALE_BASE : SCALE_BASE;
    }

    protected override float GetAlpha()
    {
        if (Progress < ALPHA_FADE) return Progress / ALPHA_FADE;
        else return (1 - Progress) / (1 - ALPHA_FADE);
    }

    protected override Vector3 GetPos() => radial + (center - radial) * Progress;

    protected override Vector3 GetScale()
    {
        var scale = scaleBase * (1 + Progress) / 2;
        return new(scale, scale, 1);
    }

    protected override Quaternion GetRotation()
    {
        var angle = rotBase + REVOLUTIONS * 360 * Progress;
        return Quaternion.Euler(0, 0, angle);
    }

    protected override RegenerateParticle Self() => this;
}

internal class Regenerator : MonoBehaviour
{
    public const float QUICK_FOCUS_SPEEDUP = 1.75f;
    public const float DEEP_FOCUS_SLOWDOWN = 1.6f;

    private const float PARTICLE_RATE = 65f;
    private const float X_OFFSET = 0.27f;
    private const float Y_OFFSET = 0.38f;

    private RegenerateParticleFactory particleFactory = new();
    private RandomFloatTicker ticker = new(0.8f / PARTICLE_RATE, 1.2f / PARTICLE_RATE);
    private HeroController? heroController;
    private bool particlesEnabled;

    internal void SetEnabled(HeroController heroController, bool particlesEnabled)
    {
        this.heroController = heroController;
        this.particlesEnabled = SeinMod.OriActive() && particlesEnabled;
    }

    private void Update()
    {
        if (!particlesEnabled) return;

        var t = heroController.gameObject.transform;
        var pos = t.position;
        pos.x += X_OFFSET * Mathf.Sign(t.localScale.x);
        pos.y += Y_OFFSET;

        float speedup = 1;
        if (PlayerDataCache.Instance.QuickFocusEquipped) speedup *= QUICK_FOCUS_SPEEDUP;
        if (PlayerDataCache.Instance.DeepFocusEquipped) speedup /= DEEP_FOCUS_SLOWDOWN;
        foreach (var elapsed in ticker.Tick(Time.deltaTime * speedup)) particleFactory.Launch(elapsed / speedup, pos);
    }
}

internal class Regenerate
{
    private static Regenerator regenerator = CreateRegenerator();

    private static Regenerator CreateRegenerator()
    {
        GameObject obj = new("Regenerator");
        Object.DontDestroyOnLoad(obj);
        var regen = obj.AddComponent<Regenerator>();
        return regen;
    }

    public static void Hook()
    {
        SceneHooks.Hook(SetFocusLines);
        On.HeroController.StartMPDrain += StartRegenerate;
        On.HeroController.StopMPDrain += StopRegenerate;
    }

    private static void SetFocusLines(bool oriEnabled, SeinSettings settings)
    {
        GOFinder.Knight().FindChild("Focus Effects").FindChild("Lines Anim").active = !oriEnabled;
    }

    private static void StartRegenerate(On.HeroController.orig_StartMPDrain orig, HeroController self, float time)
    {
        orig(self, time);
        regenerator.SetEnabled(self, true);
    }

    private static void StopRegenerate(On.HeroController.orig_StopMPDrain orig, HeroController self)
    {
        orig(self);
        regenerator.SetEnabled(self, false);
    }
}
