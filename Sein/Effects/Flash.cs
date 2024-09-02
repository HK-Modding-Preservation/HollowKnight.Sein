using HutongGames.PlayMaker;
using Sein.Util;
using UnityEngine;

namespace Sein.Effects;

internal class FlashParticleFactory : AbstractParticleFactory<FlashParticleFactory, FlashParticle>
{
    private static IC.EmbeddedSprite sprite = new("spiritlightframeparticle");

    protected override string GetObjectName() => "FlashParticle";

    protected override Sprite GetSprite() => sprite.Value;

    private const float LIFETIME = 0.45f;

    public void Launch(float prewarm, float baseScale, Vector3 start, Vector3 stop)
    {
        if (!Launch(prewarm, LIFETIME, out var particle)) return;

        particle.SetParams(baseScale, start, stop);
        particle.Finalize(prewarm);
    }
}

internal class FlashParticle : AbstractParticle<FlashParticleFactory, FlashParticle>
{
    private const float BASE_SCALE = 0.6f;
    private const float FULL_SCALE = 0.9f;

    private float baseScale;
    private Vector3 start;
    private Vector3 stop;

    internal void SetParams(float baseScale, Vector3 start, Vector3 stop)
    {
        this.baseScale = baseScale;
        this.start = start;
        this.stop = stop;
    }

    private float TrigProgress => (1 - Mathf.Cos(Progress * 2 * Mathf.PI)) / 2;

    protected override float GetAlpha() => TrigProgress;

    protected override Vector3 GetPos() => start + (stop - start) * Progress;

    protected override Vector3 GetScale()
    {
        var scale = baseScale * (BASE_SCALE + (FULL_SCALE - BASE_SCALE)) * TrigProgress;
        return new(scale, scale, 1);
    }

    protected override FlashParticle Self() => this;
}

internal class Flash : MonoBehaviour
{
    private static IC.EmbeddedSprite FlashSprite = new("flashglow");

    public static void Hook()
    {
        SceneHooks.Hook(InstantiateFlash);
        ItemChanger.Events.AddFsmEdit(new("Vignette", "Darkness Control"), MonitorLanternGlow);
    }

    private static FsmInt? darknessLevel;

    private static void MonitorLanternGlow(PlayMakerFSM fsm) => darknessLevel = fsm.FsmVariables.FindFsmInt("Darkness Level");

    private static void InstantiateFlash(bool oriEnabled, SeinSettings settings)
    {
        if (!oriEnabled) return;

        GameObject flash = new("OriFlash");
        flash.transform.localScale = new(0, 0, 1);
        flash.transform.localPosition = new(-100, -100, 0);
        flash.AddComponent<Flash>();

        var spriteRenderer = flash.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = FlashSprite.Value;
    }

    private static readonly Vector3 OFFSET = new(0, -0.45f, 0);

    private const float GROW_DURATION = 0.45f;
    private const float SHRINK_DURATION = 0.9f;
    private ProgressFloat progress = new(0, 1 / GROW_DURATION, 1 / SHRINK_DURATION);

    private const float FULL_SCALE = 1.375f;

    private const float ROT_SPEED = 360f / 8f;
    private float angle = 0;

    private const float PARTICLE_RATE = 75f;
    private const float PARTICLE_RADIUS = 3.2f;
    private const float PARTICLE_DIST_MIN = 0.5f;
    private const float PARTICLE_DIST_MAX = 0.7f;

    private FlashParticleFactory particleFactory = new();
    private RandomFloatTicker particleTicker = new(0.9f / PARTICLE_RATE, 1.1f / PARTICLE_RATE);

    private void Update()
    {
        bool glowEnabled = false;
        if (darknessLevel != null)
        {
            var hasLantern = PlayerDataCache.Instance.HasLantern;
            glowEnabled = hasLantern && darknessLevel.Value == 2;
        }
        progress.Advance(Time.deltaTime, glowEnabled ? 1 : 0);

        var scale = FULL_SCALE * (1 + Mathf.Sin((progress.Value * 2 - 1) * Mathf.PI / 2)) / 2f;
        var scaleRatio = scale / FULL_SCALE;
        transform.localScale = new(scale, scale, 1);
        transform.position = GOFinder.Knight().transform.position + OFFSET;

        angle += ROT_SPEED * Time.deltaTime;
        while (angle >= 360) angle -= 360;
        transform.localRotation = Quaternion.Euler(0, 0, angle);

        foreach (var elapsed in particleTicker.Tick(Time.deltaTime * scaleRatio))
        {
            var radius = Mathf.Sqrt(Random.Range(0f, 1f));
            var angle1 = Random.Range(0f, 360f);
            var angle2 = Random.Range(0f, 360f);
            var dist = Quaternion.Euler(0, 0, angle2) * new Vector3(Random.Range(PARTICLE_DIST_MIN, PARTICLE_DIST_MAX), 0, 0);

            var cPos = transform.position + Quaternion.Euler(0, 0, angle1) * new Vector3(radius * PARTICLE_RADIUS * scaleRatio, 0, 0);
            var start = cPos - dist / 2;
            var end = cPos + dist / 2;

            start.z = -0.02f;
            end.z = -0.02f;
            particleFactory.Launch(elapsed, scaleRatio, start, end);
        }
    }
}
