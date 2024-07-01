using Sein.Util;
using System.Collections.Generic;
using UnityEngine;

namespace Sein.Hud;

internal class SineWaveParticleFactory : UIParticleFactory<SineWaveParticleFactory, SineWaveParticle>
{
    private readonly bool lifeHud;

    internal SineWaveParticleFactory(bool lifeHud) => this.lifeHud = lifeHud;

    protected override string GetObjectName() => "SineWave";

    protected override Sprite GetSprite() => SineWaveParticle.sprite1.Value;

    protected override int SortingOrder => UICellSortingOrder.SINE_WAVE_ORDER;

    public void Launch(float prewarm, Transform parent, int dir, float speed, float fadeDist, float fadeLength, Color color, bool up)
    {
        if (!Launch(prewarm, (fadeDist + fadeLength) / speed, out var particle)) return;

        particle.SetParams(lifeHud, parent, dir, fadeDist, fadeLength, color, up);
        particle.Finalize(prewarm);
    }
}

internal class SineWaveParticle : AbstractParticle<SineWaveParticleFactory, SineWaveParticle>
{
    public const float SCALE_X = 0.45f;
    public const float SCALE_Y = 1.25f;
    private const float OVERCHARM_TIME = 1f;

    internal static IC.EmbeddedSprite sprite1 = new("sinewave1");
    internal static IC.EmbeddedSprite sprite2 = new("sinewave2");

    private bool lifeHud;
    private SpriteRenderer spriteRenderer;
    private float fadeProg;
    private float baseAlpha;
    private Vector3 target;

    internal void SetParams(bool lifeHud, Transform parent, int dir, float fadeDist, float fadeLength, Color color, bool up)
    {
        spriteRenderer ??= gameObject.GetComponent<SpriteRenderer>();

        this.lifeHud = lifeHud;
        fadeProg = fadeDist / (fadeDist + fadeLength);
        target = new(dir * (fadeDist + fadeLength), 0, 0);
        spriteRenderer.color = color;
        spriteRenderer.sprite = (up ? sprite1 : sprite2).Value;
        baseAlpha = color.a;
        transform.parent = parent;
    }

    protected override bool UseLocalPos => true;

    protected override float GetAlpha()
    {
        if (Progress < fadeProg) return baseAlpha;
        else return baseAlpha * (1 - (Progress - fadeProg) / (1 - fadeProg));
    }

    protected override Vector3 GetPos() => Progress * target;

    protected override Vector3 GetScale() => new(SCALE_X, SCALE_Y * (0.1f + 0.9f * Mathf.Pow(RProgress, 0.65f)), 1);

    private Color? origColor;
    private ProgressFloat overcharmProg = new(0, 1, 1);

    protected override bool UpdateForTime(float time)
    {
        if (!base.UpdateForTime(time))
        {
            if (origColor != null) spriteRenderer.color = origColor.Value;
            return false;
        }

        if (!lifeHud) return true;

        origColor ??= spriteRenderer.color;
        var oc = origColor.Value;
        oc.a = GetAlpha();
        bool overcharmed = PlayerDataCache.instance.Overcharmed;
        overcharmProg.Advance(time, overcharmed ? OVERCHARM_TIME : 0);
        var target = Color.magenta.Darker(0.1f);
        target.a = 0.8f * (GetAlpha() / 0.5f);
        spriteRenderer.color = oc.Interpolate(overcharmProg.Value / OVERCHARM_TIME, target);

        return true;
    }

    protected override SineWaveParticle Self() => this;
}

internal class SineWaveLauncher
{
    private static readonly float SPEED = 0.2f;
    private static readonly float SPAN = 0.67f;
    private static float RATE = SPEED / SPAN;

    private readonly SineWaveParticleFactory particleFactory;
    private readonly RandomFloatTicker ticker = new(1 / RATE, 1 / RATE);
    private bool up = true;

    internal SineWaveLauncher(bool lifeHud) => this.particleFactory = new(lifeHud);

    public void Update(float time, Transform parent, int dir, float fadeDist, float fadeLength, Color color)
    {
        foreach (var elapsed in ticker.Tick(time))
        {
            particleFactory.Launch(elapsed, parent, dir, SPEED, fadeDist, fadeLength, color, up);
            up = !up;
        }
    }
}

public enum UICellParticleMode
{
    Inwards,
    Outwards,
    Drip,
}

internal class UICellParticleFactory : UIParticleFactory<UICellParticleFactory, UICellParticle>
{
    private static IC.EmbeddedSprite sprite = new("cellbody");

    protected override string GetObjectName() => "CellParticle";

    protected override Sprite GetSprite() => sprite.Value;

    protected override int SortingOrder => UICellSortingOrder.PARTICLE_ORDER;

    public void Launch(float prewarm, Transform parent, Color color, float time, UICellParticleMode mode)
    {
        if (!Launch(prewarm, time, out var particle)) return;

        particle.gameObject.transform.parent = parent;
        particle.gameObject.GetComponent<SpriteRenderer>().color = color;
        particle.SetParams(mode);
        particle.Finalize(prewarm);
    }
}

internal class UICellParticle : AbstractParticle<UICellParticleFactory, UICellParticle>
{
    private static float RADIAL_SPAWN_MIN = 1.25f;
    private static float RADIAL_SPAWN_MAX = 1.75f;
    private static float RADIAL_INTERIOR_SPAWN_MAX = 0.8f;
    private static float RADIAL_DRIP_MIN = 1.65f;
    private static float RADIAL_DRIP_MAX = 2.65f;
    private static float BURST_SCALE_MIN = 0.25f;
    private static float BURST_SCALE_MAX = 0.35f;
    private static float DRIP_SCALE_MIN = 0.15f;
    private static float DRIP_SCALE_MAX = 0.2f;

    private UICellParticleMode mode;
    private float angle;
    private float spawnRadius;
    private float scaleMult;
    private float drip;

    protected override bool UseLocalPos => true;

    internal void SetParams(UICellParticleMode mode)
    {
        this.mode = mode;
        this.angle = Random.Range(0f, 360f);

        if (mode == UICellParticleMode.Drip)
        {
            spawnRadius = Mathf.Sqrt(Random.Range(0f, 1f)) * RADIAL_INTERIOR_SPAWN_MAX;
            drip = Random.Range(RADIAL_DRIP_MIN, RADIAL_DRIP_MAX);
            scaleMult = Random.Range(DRIP_SCALE_MIN, DRIP_SCALE_MAX);
        }
        else
        {
            spawnRadius = Random.Range(RADIAL_SPAWN_MIN, RADIAL_SPAWN_MAX);
            scaleMult = Random.Range(BURST_SCALE_MIN, BURST_SCALE_MAX);
        }
    }

    private float AlphaValue()
    {
        switch (mode)
        {
            case UICellParticleMode.Inwards: return Mathf.Sqrt(Progress);
            case UICellParticleMode.Outwards: return Mathf.Sqrt(RProgress);
            case UICellParticleMode.Drip: return RProgress;
            default: return Progress;
        }
    }

    private float ScaleValue()
    {
        if (mode == UICellParticleMode.Drip) return Mathf.Sqrt(RProgress);
        else return AlphaValue();
    }

    protected override float GetAlpha() => AlphaValue();

    private (Vector3, Vector3) StartEndPos()
    {
        Vector3 fwd = new(spawnRadius, 0, 0);
        if (mode == UICellParticleMode.Drip)
        {
            Vector3 dripVec = new(0, -drip, 0);

            var spawn = Quaternion.Euler(0, 0, angle) * fwd;
            return (spawn, spawn + dripVec);
        }
        else
        {
            var outer = Quaternion.Euler(0, 0, angle) * fwd;
            if (mode == UICellParticleMode.Inwards) return (outer, Vector3.zero);
            else return (Vector3.zero, outer);
        }
    }

    protected override Vector3 GetPos()
    {
        var (start, end) = StartEndPos();
        var prog = mode == UICellParticleMode.Drip ? Progress : Mathf.Sqrt(Progress);
        return start + (end - start) * prog;
    }

    protected override Vector3 GetScale()
    {
        var scale = ScaleValue() * scaleMult;
        return new(scale, scale, 1);
    }

    protected override UICellParticle Self() => this;
}

internal static class AbstractUICellFactory
{
    public static C Create<C, T>(UICellParticleFactory particleFactory, C? previousCell, int index, T state) where C : AbstractUICell<C, T>
    {
        GameObject obj = new("Cell");
        var cell = obj.AddComponent<C>();
        cell.InitImpl(particleFactory, previousCell, index, state);

        return cell;
    }
}

internal static class UICellSortingOrder
{
    public const int SINE_WAVE_ORDER = -3;
    public const int BG_ORDER = -2;
    public const int BODY_ORDER = -1;
    public const int PARTICLE_ORDER = 1;
    public const int FRAME_ORDER = 2;
    public const int COVER_ORDER = 3;
}

internal abstract class AbstractUICell<C, T> : MonoBehaviour where C : AbstractUICell<C, T>
{
    private static IC.EmbeddedSprite bgSprite = new("cellbg");
    private static IC.EmbeddedSprite bodySprite = new("cellbody");
    private static IC.EmbeddedSprite frameSprite = new("cellframe");

    private const float LARGE_CELL_SCALE = 0.65f;
    private const float SMALL_CELL_SCALE = 0.425f;
    private static float BG_ALPHA = 0.75f;
    private static float COVER_GRAY = 0.95f;
    private static float COVER_ALPHA = 0.45f;
    private static Color COVER_COLOR = new(COVER_GRAY, COVER_GRAY, COVER_GRAY, COVER_ALPHA);

    private UICellParticleFactory particleFactory;
    protected C? previousCell;

    private GameObject scaleContainer;
    private GameObject bg;
    private SpriteRenderer bgRenderer;
    private GameObject body;
    private SpriteRenderer bodyRenderer;
    private GameObject frame;
    private SpriteRenderer frameRenderer;
    private GameObject cover;
    private SpriteRenderer coverRenderer;

    protected static Color Hex(int r, int g, int b) => new(r / 255f, g / 255f, b / 255f);

    protected abstract T DefaultState();

    protected abstract Color GetBodyColor(T state);

    protected abstract Color GetFrameColor(T state);

    protected abstract Sprite GetCoverSprite(int index);

    private static int ModPow(int b, int p, int m)
    {
        int x = 1;
        for (int i = 0; i < p; i++) x = (x * b) % m;
        return x;
    }

    internal void InitImpl(UICellParticleFactory particleFactory, C? previousCell, int index, T state)
    {
        this.particleFactory = particleFactory;
        this.previousCell = previousCell;

        prevState = DefaultState();
        prevBodyColor = GetBodyColor(state);
        prevFrameColor = GetFrameColor(state);
        targetState = state;
        targetBodyColor = prevBodyColor;
        targetFrameColor = prevFrameColor;

        scaleContainer = new("CellScaleContainer");
        scaleContainer.transform.parent = transform;
        var scale = previousCell != null ? SMALL_CELL_SCALE : LARGE_CELL_SCALE;
        scaleContainer.transform.localScale = new(scale, scale, 1);

        (bg, bgRenderer) = UISprites.CreateUISprite("CellBG", bgSprite.Value, UICellSortingOrder.BG_ORDER);
        bg.transform.parent = scaleContainer.transform;
        bgRenderer.SetAlpha(BG_ALPHA);
        bg.SetActive(false);

        (body, bodyRenderer) = UISprites.CreateUISprite("CellBody", bodySprite.Value, UICellSortingOrder.BODY_ORDER);
        body.transform.parent = scaleContainer.transform;
        bodyRenderer.color = prevBodyColor;
        body.SetActive(false);

        (frame, frameRenderer) = UISprites.CreateUISprite("CellFrame", frameSprite.Value, UICellSortingOrder.FRAME_ORDER);
        frame.transform.parent = scaleContainer.transform;
        frame.SetActive(false);

        (cover, coverRenderer) = UISprites.CreateUISprite("CellCover", GetCoverSprite(index), UICellSortingOrder.COVER_ORDER);
        cover.transform.parent = scaleContainer.transform;
        cover.transform.localRotation = Quaternion.Euler(0, 0, index == 0 ? 0 : (ModPow(5, (index - 1), 23) * 15));
        coverRenderer.color = COVER_COLOR;
        cover.SetActive(false);
    }

    private ProgressFloat bgSize = new(0, 4.5f, 10f);

    private float TargetBGSize(T state)
    {
        if (previousCell != null && previousCell.bgSize.Value < 1) return 0;
        else if (!StateIsPermanent(state)) return 0;
        else return 1;
    }

    private float SyncBG(T state)
    {
        bgSize.Advance(Time.deltaTime, TargetBGSize(state));
        var scale = bgSize.Value;

        bg.transform.localScale = new(scale, scale, 1);
        bg.SetActive(scale > 0);
        return scale;
    }

    protected T prevState;
    protected T? targetState;
    protected Color prevBodyColor;
    protected Color prevFrameColor;
    protected Color? targetBodyColor;
    protected Color? targetFrameColor;
    protected ProgressFloat bodyProgress = new(0, 1, 1);

    protected abstract float ComputeBodyScale(T state);

    private float ComputeBodyScale()
    {
        float prevScale = ComputeBodyScale(prevState);
        float newScale = ComputeBodyScale(targetState);
        return prevScale + (newScale - prevScale) * bodyProgress.Value;
    }

    private Color ComputeBodyColor() => prevBodyColor.Interpolate(bodyProgress.Value, targetBodyColor.Value);

    private Color ComputeFrameColor() => prevFrameColor.Interpolate(bodyProgress.Value, targetFrameColor.Value);

    protected abstract float BodyAdvanceSpeed();

    protected abstract bool StateIsPermanent(T state);

    private float SyncBody(T state)
    {
        targetState ??= state;
        if (!EqualityComparer<T>.Default.Equals(targetState, state))
        {
            // Reset progress.
            bodyProgress.Value = 0;

            prevState = targetState;
            prevBodyColor = bodyRenderer.color;
            prevFrameColor = frameRenderer.color;

            targetState = state;
            targetBodyColor = GetBodyColor(state);
            targetFrameColor = GetFrameColor(state);
        }
        bodyProgress.Advance(Time.deltaTime * BodyAdvanceSpeed(), 1);
        if (bodyProgress.Value == 1) prevState = targetState;

        var scale = ComputeBodyScale();

        if (StateIsPermanent(state)) scale *= bgSize.Value;
        if (scale > 0)
        {
            body.transform.localScale = new(scale, scale, 1);
            bodyRenderer.color = ComputeBodyColor();
            body.SetActive(true);

            frame.transform.localScale = new(scale, scale, 1);
            frameRenderer.color = ComputeFrameColor();
            frame.SetActive(true);
        }
        else
        {
            body.SetActive(false);
            frame.SetActive(false);
        }

        return scale;
    }

    private void SyncCover(float a, float b)
    {
        float scale = Mathf.Max(a, b);
        cover.SetActive(scale > 0);
        cover.transform.localScale = new(scale, scale, 1);
    }

    protected static RandomFloatTicker RatedTicker(float rate) => new(0.9f / rate, 1.1f / rate);

    protected void TickParticles(RandomFloatTicker ticker, float deltaTime, float time, Color color, UICellParticleMode mode)
    {
        foreach (var elapsed in ticker.Tick(deltaTime))
            particleFactory.Launch(elapsed, scaleContainer.transform, color, time, mode);
    }

    protected abstract void EmitParticles(float bodySize);

    public void Sync(T state)
    {
        var a = SyncBG(state);
        var b = SyncBody(state);
        SyncCover(a, b);
        EmitParticles(b);
    }
}

internal abstract class AbstractCellHud<C, T> : MonoBehaviour where C : AbstractUICell<C, T>
{
    private static float FIRST_OFFSET_X = 2.5f;
    private static float SECOND_OFFSET_X = 0.95f;
    private static float SMALL_OFFSET_X = 0.77f;
    private static float LARGE_OFFSET_X = 1.15f;

    private static List<float> offsets = [FIRST_OFFSET_X];

    protected abstract int OffsetSign();

    private float OffsetX(int index)
    {
        while (offsets.Count <= index) offsets.Add(offsets[offsets.Count - 1] + IncOffsetX(offsets.Count));
        return offsets[index] * OffsetSign();
    }

    private float IncOffsetX(int index)
    {
        if (index == 1) return SECOND_OFFSET_X;
        else if (index % 3 != 0) return SMALL_OFFSET_X;
        else return LARGE_OFFSET_X;
    }

    protected abstract float SineWaveDist();

    protected abstract float SineWaveFade();

    protected abstract Color SineWaveColor();

    private static readonly float OFFSET_Y = 0;

    private SineWaveLauncher sineWaveLauncher;
    private UICellParticleFactory particleFactory = new();
    protected List<C> cells = new();

    protected abstract List<T> GetCellStates();

    protected abstract T EmptyCellState();

    private void AddCell(T state)
    {
        var newCell = AbstractUICellFactory.Create(particleFactory, cells.Count == 0 ? null : cells[cells.Count - 1], cells.Count, state);
        
        newCell.transform.parent = transform;
        newCell.transform.localPosition = new(OffsetX(cells.Count), OFFSET_Y, 0);
        cells.Add(newCell);
    }

    private float sineWavePrewarm = 60;

    protected void Update()
    {
        var cellStates = GetCellStates();

        // Sync all cell states.
        for (int i = 0; i < cells.Count; i++)
        {
            var state = i < cellStates.Count ? cellStates[i] : EmptyCellState();
            cells[i].Sync(state);
        }
        // Add any missing cells.
        while (cells.Count < cellStates.Count) AddCell(cellStates[cells.Count]);

        sineWaveLauncher ??= new(OffsetSign() == 1);
        sineWaveLauncher.Update(Time.deltaTime + sineWavePrewarm, transform.parent, OffsetSign(), SineWaveDist(), SineWaveFade(), SineWaveColor());
        sineWavePrewarm = 0;
    }
}
