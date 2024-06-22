using Sein.Util;
using System.Collections.Generic;
using UnityEngine;

namespace Sein.Hud;

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

    protected override int SortingOrder => 1;

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

internal abstract class AbstractUICell<C, T> : MonoBehaviour where C : AbstractUICell<C, T>
{
    private static IC.EmbeddedSprite bgSprite = new("cellbg");
    private static IC.EmbeddedSprite bodySprite = new("cellbody");
    private static IC.EmbeddedSprite frameSprite = new("cellframe");

    private const float LARGE_CELL_SCALE = 0.65f;
    private const float SMALL_CELL_SCALE = 0.425f;

    private const int BG_ORDER = -2;
    private const int BODY_ORDER = -1;
    private const int PARTICLE_ORDER = 1;  // Must be 1.
    private const int FRAME_ORDER = 2;
    private const int COVER_ORDER = 3;
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

        (bg, bgRenderer) = UISprites.CreateUISprite("CellBG", bgSprite.Value, BG_ORDER);
        bg.transform.parent = scaleContainer.transform;
        bgRenderer.SetAlpha(BG_ALPHA);
        bg.SetActive(false);

        (body, bodyRenderer) = UISprites.CreateUISprite("CellBody", bodySprite.Value, BODY_ORDER);
        body.transform.parent = scaleContainer.transform;
        bodyRenderer.color = prevBodyColor;
        body.SetActive(false);

        (frame, frameRenderer) = UISprites.CreateUISprite("CellFrame", frameSprite.Value, FRAME_ORDER);
        frame.transform.parent = scaleContainer.transform;
        frame.SetActive(false);

        (cover, coverRenderer) = UISprites.CreateUISprite("CellCover", GetCoverSprite(index), COVER_ORDER);
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

    private const int MIN_TICKS = 90;
    private const int MAX_TICKS = 110;

    protected static PeriodicFloatTicker RatedTicker(float rate) => new(1, Mathf.FloorToInt(rate * (MIN_TICKS + MAX_TICKS) / 2), MIN_TICKS, MAX_TICKS);

    protected void TickParticles(PeriodicFloatTicker ticker, float deltaTime, float time, Color color, UICellParticleMode mode)
    {
        foreach (var elapsed in ticker.TickFloats(deltaTime))
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


    private static readonly float OFFSET_Y = 0;

    private UICellParticleFactory particleFactory = new();
    private List<C> cells = new();

    protected abstract List<T> GetCellStates();

    protected abstract T EmptyCellState();

    private void AddCell(T state)
    {
        var newCell = AbstractUICellFactory.Create(particleFactory, cells.Count == 0 ? null : cells[cells.Count - 1], cells.Count, state);
        
        newCell.transform.parent = transform;
        newCell.transform.localPosition = new(OffsetX(cells.Count), OFFSET_Y, 0);
        cells.Add(newCell);
    }

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
    }
}
