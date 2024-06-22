using Sein.Util;
using System.Collections.Generic;
using UnityEngine;

namespace Sein.Hud;

internal enum LifeCellFillState
{
    Empty,
    Healing,
    Filled
}

internal record LifeCellState
{
    public LifeCellFillState fillState;
    public bool permanent;
    public bool hiveblood;
    public bool lifeblood;
    public bool furied;
}

internal class LifeStateTracker
{
    private List<LifeCellState> cellStates;

    public LifeStateTracker()
    {
        cellStates = new();
    }

    public void Init() { }

    public void Unload() { }

    public void CheckPlayerData()
    {
        var pd = PlayerData.instance;

        var health = pd.GetInt(nameof(PlayerData.health));
        var maxHealth = pd.GetInt(nameof(PlayerData.maxHealth));
        var healthBlue = pd.GetInt(nameof(PlayerData.healthBlue));
        bool hiveblood = HivebloodWatcher.HivebloodEquipped;
        bool hivebloodHealing = HivebloodWatcher.HivebloodHealing;
        bool jonis = pd.GetBool(nameof(PlayerData.equippedCharm_27));
        bool furied = false;  // TODO: Handle fury

        cellStates.Clear();
        if (health == 0)
        {
            maxHealth = jonis ? 1 : maxHealth;
            for (int i = 0; i < maxHealth; i++) cellStates.Add(new()
            {
                fillState = LifeCellFillState.Empty,
                permanent = true,
            });
        }
        else if (jonis)
        {
            // Base health is always 1.
            cellStates.Add(new()
            {
                fillState = LifeCellFillState.Filled,
                permanent = true,
                hiveblood = hiveblood,
                lifeblood = true,
                furied = furied,
            });

            for (int i = 0; i < healthBlue; i++) cellStates.Add(new()
            {
                fillState = LifeCellFillState.Filled,
                hiveblood = hiveblood,
                lifeblood = true,
                furied = furied,
            });

            if (hivebloodHealing) cellStates.Add(new()
            {
                fillState = LifeCellFillState.Healing,
                lifeblood = true,
                hiveblood = true,
                furied = furied
            });
        }
        else
        {
            for (int i = 0; i < maxHealth; i++) cellStates.Add(new()
            {
                fillState = i < health ? LifeCellFillState.Filled : LifeCellFillState.Empty,
                permanent = true,
                hiveblood = hiveblood,
                furied = furied && i == 0,
            });

            for (int i = 0; i < healthBlue; i++) cellStates.Add(new()
            {
                fillState = LifeCellFillState.Filled,
                lifeblood = true,
            });

            if (hivebloodHealing) cellStates[health].fillState = LifeCellFillState.Healing;
        }
    }

    public List<LifeCellState> GetCellStates() => new(cellStates);
}

public enum LifeCellParticleMode
{
    Inwards,
    Outwards,
    Drip,
}

internal class LifeCellParticleFactory : UIParticleFactory<LifeCellParticleFactory, LifeCellParticle>
{
    private static IC.EmbeddedSprite sprite = new("cellbody");

    protected override string GetObjectName() => "LifeCellParticle";

    protected override Sprite GetSprite() => sprite.Value;

    protected override int SortingOrder => LifeCell.PARTICLE_ORDER;

    public void Launch(float prewarm, Transform parent, Color color, float time, LifeCellParticleMode mode)
    {
        if (!Launch(prewarm, time, out var particle)) return;

        particle.gameObject.transform.parent = parent;
        particle.gameObject.GetComponent<SpriteRenderer>().color = color;
        particle.SetParams(mode);
        particle.Finalize(prewarm);
    }
}

internal class LifeCellParticle : AbstractParticle<LifeCellParticleFactory, LifeCellParticle>
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

    private LifeCellParticleMode mode;
    private float angle;
    private float spawnRadius;
    private float scaleMult;
    private float drip;

    protected override bool UseLocalPos => true;

    internal void SetParams(LifeCellParticleMode mode)
    {
        this.mode = mode;
        this.angle = Random.Range(0f, 360f);

        if (mode == LifeCellParticleMode.Drip)
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
            case LifeCellParticleMode.Inwards: return Mathf.Sqrt(Progress);
            case LifeCellParticleMode.Outwards: return Mathf.Sqrt(RProgress);
            case LifeCellParticleMode.Drip: return RProgress;
            default: return Progress;
        }
    }

    private float ScaleValue()
    {
        if (mode == LifeCellParticleMode.Drip) return Mathf.Sqrt(RProgress);
        else return AlphaValue();
    }

    protected override float GetAlpha() => AlphaValue();

    private (Vector3, Vector3) StartEndPos()
    {
        Vector3 fwd = new(spawnRadius, 0, 0);
        if (mode == LifeCellParticleMode.Drip)
        {
            Vector3 dripVec = new(0, -drip, 0);

            var spawn = Quaternion.Euler(0, 0, angle) * fwd;
            return (spawn, spawn + dripVec);
        }
        else
        {
            var outer = Quaternion.Euler(0, 0, angle) * fwd;
            if (mode == LifeCellParticleMode.Inwards) return (outer, Vector3.zero);
            else return (Vector3.zero, outer);
        }
    }

    protected override Vector3 GetPos()
    {
        var (start, end) = StartEndPos();
        var prog = mode == LifeCellParticleMode.Drip ? Progress : Mathf.Sqrt(Progress);
        return start + (end - start) * prog;
    }

    protected override Vector3 GetScale()
    {
        var scale = ScaleValue() * scaleMult;
        return new(scale, scale, 1);
    }

    protected override LifeCellParticle Self() => this;
}

internal class LifeCell : MonoBehaviour
{
    private static IC.EmbeddedSprite bgSprite = new("cellbg");
    private static IC.EmbeddedSprite bodySprite = new("cellbody");
    private static IC.EmbeddedSprite frameSprite = new("cellframe");
    private static IC.EmbeddedSprite firstCover = new("lifecell1cover");
    private static IC.EmbeddedSprite otherCover = new("lifecellncover");

    private const float LARGE_CELL_SCALE = 0.65f;
    private const float SMALL_CELL_SCALE = 0.425f;

    private const int BG_ORDER = -2;
    private const int BODY_ORDER = -1;
    internal const int PARTICLE_ORDER = 1;
    private const int FRAME_ORDER = 2;
    private const int COVER_ORDER = 3;
    private static float BG_ALPHA = 0.75f;
    private static float COVER_GRAY = 0.95f;
    private static float COVER_ALPHA = 0.45f;
    private static Color COVER_COLOR = new(COVER_GRAY, COVER_GRAY, COVER_GRAY, COVER_ALPHA);

    private LifeCellParticleFactory particleFactory;

    private LifeCell? previous;

    private GameObject scaleContainer;
    private GameObject bg;
    private SpriteRenderer bgRenderer;
    private GameObject body;
    private SpriteRenderer bodyRenderer;
    private GameObject frame;
    private SpriteRenderer frameRenderer;
    private GameObject cover;
    private SpriteRenderer coverRenderer;

    private static Color Hex(int r, int g, int b) => new(r / 255f, g / 255f, b / 255f);

    private static readonly Color FURY_COLOR = Hex(170, 44, 22);
    private static readonly Color LIFEBLOOD_COLOR = Hex(93, 183, 209);
    private static readonly Color HIVEBLOOD_COLOR = Hex(245, 153, 52);
    private static readonly Color LIFE_COLOR = Hex(201, 233, 97);

    private Color GetBodyColor(LifeCellState state)
    {
        if (state.furied) return FURY_COLOR;
        else if (state.lifeblood) return LIFEBLOOD_COLOR;
        else if (state.hiveblood) return HIVEBLOOD_COLOR;
        else return LIFE_COLOR;
    }

    private Color GetFrameColor(LifeCellState state)
    {
        if (state.furied) return FURY_COLOR.Darker(0.4f);
        else if (state.hiveblood) return HIVEBLOOD_COLOR.Darker(state.lifeblood ? 0.15f : 0.25f);
        else if (state.lifeblood) return LIFEBLOOD_COLOR.Darker(0.25f);
        else return LIFE_COLOR.Darker(0.2f);
    }

    private static int ModPow(int b, int p, int m)
    {
        int x = 1;
        for (int i = 0; i < p; i++) x = (x * b) % m;
        return x;
    }

    private void InitImpl(LifeCellParticleFactory particleFactory, LifeCell? previous, int index, LifeCellState state)
    {
        this.particleFactory = particleFactory;
        this.previous = previous;

        prevState = new() { fillState = LifeCellFillState.Empty };
        prevBodyColor = GetBodyColor(state);
        prevFrameColor = GetFrameColor(state);
        targetState = state;
        targetBodyColor = prevBodyColor;
        targetFrameColor = prevFrameColor;

        scaleContainer = new("LifeCellScaleContainer");
        scaleContainer.transform.parent = transform;
        var scale = previous != null ? SMALL_CELL_SCALE : LARGE_CELL_SCALE;
        scaleContainer.transform.localScale = new(scale, scale, 1);

        (bg, bgRenderer) = UISprites.CreateUISprite("LifeCellBG", bgSprite.Value, BG_ORDER);
        bg.transform.parent = scaleContainer.transform;
        bgRenderer.SetAlpha(BG_ALPHA);
        bg.SetActive(false);

        (body, bodyRenderer) = UISprites.CreateUISprite("LifeCellBody", bodySprite.Value, BODY_ORDER);
        body.transform.parent = scaleContainer.transform;
        bodyRenderer.color = prevBodyColor;
        body.SetActive(false);

        (frame, frameRenderer) = UISprites.CreateUISprite("LifeCellFrame", frameSprite.Value, FRAME_ORDER);
        frame.transform.parent = scaleContainer.transform;
        frame.SetActive(false);

        (cover, coverRenderer) = UISprites.CreateUISprite("LifeCellCover", (previous == null ? firstCover : otherCover).Value, COVER_ORDER);
        cover.transform.parent = scaleContainer.transform;
        cover.transform.localRotation = Quaternion.Euler(0, 0, index == 0 ? 0 : (ModPow(5, (index - 1), 23) * 15));
        coverRenderer.color = COVER_COLOR;
        cover.SetActive(false);
    }

    public static LifeCell Create(LifeCellParticleFactory particleFactory, LifeCell? previous, int index, LifeCellState state)
    {
        GameObject obj = new("LifeCell");
        var lifeCell = obj.AddComponent<LifeCell>();
        lifeCell.InitImpl(particleFactory, previous, index, state);

        return lifeCell;
    }

    private static float BG_SIZE = 1;
    private ProgressFloat bgSize = new(0, 4.5f, 10f);

    private float TargetBGSize(LifeCellState state)
    {
        if (previous != null && previous.bgSize.Value < BG_SIZE) return 0;
        else if (!state.permanent) return 0;
        else return BG_SIZE;
    }

    private float SyncBG(LifeCellState state)
    {
        bgSize.Advance(Time.deltaTime, TargetBGSize(state));
        var scale = bgSize.Value;

        bg.transform.localScale = new(scale, scale, 1);
        bg.SetActive(scale > 0);
        return scale;
    }

    private LifeCellState prevState;
    private LifeCellState? targetState;
    private Color prevBodyColor;
    private Color prevFrameColor;
    private Color? targetBodyColor;
    private Color? targetFrameColor;
    private ProgressFloat bodyProgress = new(0, 1, 1);

    private static float ComputeBodyScale(LifeCellState state) => state.fillState == LifeCellFillState.Filled ? 1 : 0;

    private float ComputeBodyScale()
    {
        float prevScale = ComputeBodyScale(prevState);
        float newScale = ComputeBodyScale(targetState);
        return prevScale + (newScale - prevScale) * bodyProgress.Value;
    }

    private Color ComputeBodyColor() => prevBodyColor.Interpolate(bodyProgress.Value, targetBodyColor.Value);

    private Color ComputeFrameColor() => prevFrameColor.Interpolate(bodyProgress.Value, targetFrameColor.Value);

    private float SyncBody(LifeCellState state)
    {
        targetState ??= state;
        if (state != targetState)
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
        bodyProgress.Advance(Time.deltaTime * (targetState.fillState == LifeCellFillState.Empty ? 8 : 3), 1);

        var scale = ComputeBodyScale();

        if (state.permanent) scale *= bgSize.Value;
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

    private const float HEAL_PARTICLES_PER_SEC = 100;
    private const float HEAL_PARTICLES_TIME = 0.3f;
    private const float DAMAGE_PARTICLES_PER_SEC = 150;
    private const float DAMAGE_PARTICLES_TIME = 0.2f;
    private const float LIFEBLOOD_DRIP_PER_SEC = 6.5f;
    private const float LIFEBLOOD_DRIP_TIME = 1.85f;
    private const float HIVEBLOOD_DRIP_PER_SEC = 13f;
    private const float HIVEBLOOD_DRIP_TIME = 1.25f;

    private const int MIN_TICKS = 90;
    private const int MAX_TICKS = 110;

    private static PeriodicFloatTicker RatedTicker(float rate) => new(1, Mathf.FloorToInt(rate * (MIN_TICKS + MAX_TICKS) / 2), MIN_TICKS, MAX_TICKS);

    private PeriodicFloatTicker healTicker = RatedTicker(HEAL_PARTICLES_PER_SEC);
    private PeriodicFloatTicker damageTicker = RatedTicker(DAMAGE_PARTICLES_PER_SEC);
    private PeriodicFloatTicker lifebloodDripTicker = RatedTicker(LIFEBLOOD_DRIP_PER_SEC);
    private PeriodicFloatTicker hivebloodDripTicker = RatedTicker(HIVEBLOOD_DRIP_PER_SEC);

    private void TickParticles(PeriodicFloatTicker ticker, float time, Color color, LifeCellParticleMode mode)
    {
        foreach (var elapsed in ticker.TickFloats(Time.deltaTime))
            particleFactory.Launch(elapsed, scaleContainer.transform, color, time, mode);
    }

    private void EmitParticles(float bodySize)
    {
        if (bodySize > 0 && bodySize < 1)
        {
            if (targetState.fillState == LifeCellFillState.Filled)
                TickParticles(healTicker, HEAL_PARTICLES_TIME, targetBodyColor.Value, LifeCellParticleMode.Inwards);
            else
                TickParticles(damageTicker, DAMAGE_PARTICLES_TIME, prevBodyColor, LifeCellParticleMode.Outwards);
        }
        else
        {
            if (targetState.lifeblood) TickParticles(lifebloodDripTicker, LIFEBLOOD_DRIP_TIME, LIFEBLOOD_COLOR, LifeCellParticleMode.Drip);
            if (targetState.fillState == LifeCellFillState.Healing) TickParticles(hivebloodDripTicker, HIVEBLOOD_DRIP_TIME, HIVEBLOOD_COLOR, LifeCellParticleMode.Drip);
        }
    }

    public void Sync(LifeCellState state)
    {
        var a = SyncBG(state);
        var b = SyncBody(state);
        SyncCover(a, b);
        EmitParticles(b);
    }
}

internal class LifeHud : MonoBehaviour
{
    private static float FIRST_OFFSET_X = 2.5f;
    private static float SECOND_OFFSET_X = 0.95f;
    private static float SMALL_OFFSET_X = 0.77f;
    private static float LARGE_OFFSET_X = 1.15f;

    private static List<float> offsets = [FIRST_OFFSET_X];

    private static float OffsetX(int index)
    {
        while (offsets.Count <= index) offsets.Add(offsets[offsets.Count - 1] + IncOffsetX(offsets.Count));
        return offsets[index];
    }

    private static float IncOffsetX(int index)
    {
        if (index == 1) return SECOND_OFFSET_X;
        else if (index % 3 != 0) return SMALL_OFFSET_X;
        else return LARGE_OFFSET_X;
    }


    private static readonly float OFFSET_Y = 0;

    private LifeStateTracker tracker = new();
    private LifeCellParticleFactory particleFactory = new();
    private List<LifeCell> lifeCells = new();

    protected void Awake() => tracker.Init();

    protected void OnDestroy() => tracker.Unload();

    private void AddLifeCell(LifeCellState state)
    {
        var newCell = LifeCell.Create(particleFactory, lifeCells.Count == 0 ? null : lifeCells[lifeCells.Count - 1], lifeCells.Count, state);
        
        newCell.transform.parent = transform;
        newCell.transform.localPosition = new(OffsetX(lifeCells.Count), OFFSET_Y, 0);
        lifeCells.Add(newCell);
    }

    protected void Update()
    {
        tracker.CheckPlayerData();
        var lifeState = tracker.GetCellStates();

        // Sync all cell states.
        for (int i = 0; i < lifeCells.Count; i++)
        {
            LifeCellState state = i < lifeState.Count ? lifeState[i] : new() { fillState = LifeCellFillState.Empty };
            lifeCells[i].Sync(state);
        }
        // Add any missing cells.
        while (lifeCells.Count < lifeState.Count) AddLifeCell(lifeState[lifeCells.Count]);
    }
}
