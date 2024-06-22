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
            for (int i = 0; i < healthBlue; i++) cellStates.Add(new()
            {
                fillState = LifeCellFillState.Filled,
                permanent = i == 0,
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

            if (hivebloodHealing && health < maxHealth) cellStates[health].fillState = LifeCellFillState.Healing;
        }
    }

    public List<LifeCellState> GetCellStates() => new(cellStates);
}

internal class LifeCell : MonoBehaviour
{
    private static IC.EmbeddedSprite bgSprite = new("cellbg");
    private static IC.EmbeddedSprite bodySprite = new("cellbody");
    private static IC.EmbeddedSprite firstCover = new("lifecell1cover");
    private static IC.EmbeddedSprite otherCover = new("lifecellncover");

    private const float LARGE_CELL_SCALE = 0.65f;
    private const float SMALL_CELL_SCALE = 0.425f;

    private const int BG_ORDER = -2;
    private const int BODY_ORDER = -1;
    private const int COVER_ORDER = 1;
    private static float BG_ALPHA = 0.75f;
    private static Color COVER_COLOR = new(0.5f, 0.5f, 0.5f, 0.35f);

    private LifeCell? previous;
    private GameObject bg;
    private SpriteRenderer bgRenderer;
    private GameObject body;
    private SpriteRenderer bodyRenderer;
    private GameObject cover;
    private SpriteRenderer coverRenderer;

    private Color GetStateColor(LifeCellState state)
    {
        if (state.furied) return new(170, 44, 22);
        if (state.lifeblood) return new(93, 183, 209);
        else if (state.hiveblood) return new(245, 153, 52);
        return new(201, 233, 97);
    }

    private static int ModPow(int b, int p, int m)
    {
        int x = 1;
        for (int i = 0; i < p; i++) x = (x * b) % m;
        return x;
    }

    private void InitImpl(LifeCell? previous, int index, LifeCellState state)
    {
        this.previous = previous;

        prevState = state;
        prevColor = GetStateColor(state);
        targetState = state;
        targetColor = prevColor;
        bodyProgress.Value = 1;

        GameObject scaleContainer = new("LifeCellScaleContainer");
        scaleContainer.transform.parent = transform;
        var scale = previous != null ? SMALL_CELL_SCALE : LARGE_CELL_SCALE;
        scaleContainer.transform.localScale = new(scale, scale, 1);

        (bg, bgRenderer) = UISprites.CreateUISprite("LifeCellBG", bgSprite.Value, BG_ORDER);
        bg.transform.parent = scaleContainer.transform;
        bgRenderer.SetAlpha(BG_ALPHA);
        bg.gameObject.SetActive(false);

        (body, bodyRenderer) = UISprites.CreateUISprite("LifeCellBody", bodySprite.Value, BODY_ORDER);
        body.transform.parent = scaleContainer.transform;
        bodyRenderer.color = prevColor;
        body.gameObject.SetActive(false);

        (cover, coverRenderer) = UISprites.CreateUISprite("LifeCellCover", (previous == null ? firstCover : otherCover).Value, COVER_ORDER);
        cover.transform.parent = scaleContainer.transform;
        cover.transform.localRotation = Quaternion.Euler(0, 0, index == 0 ? 0 : (ModPow(5, (index - 1), 23) * 15));
        coverRenderer.color = COVER_COLOR;
        cover.gameObject.SetActive(false);
    }

    public static LifeCell Create(LifeCell? previous, int index, LifeCellState state)
    {
        GameObject obj = new("LifeCell");
        var lifeCell = obj.AddComponent<LifeCell>();
        lifeCell.InitImpl(previous, index, state);

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
    private Color prevColor;
    private Color? targetColor;
    private ProgressFloat bodyProgress = new(0, 1, 1);

    private static float ComputeBodyScale(LifeCellState state) => state.fillState == LifeCellFillState.Filled ? 0 : 1;

    private float ComputeBodyScale()
    {
        float prevScale = ComputeBodyScale(prevState);
        float newScale = ComputeBodyScale(targetState);
        return prevScale + (newScale - prevScale) * bodyProgress.Value;
    }

    private Color ComputeBodyColor() => prevColor.Interpolate(bodyProgress.Value, targetColor.Value);

    private float SyncBody(LifeCellState state)
    {
        targetState ??= state;
        if (state != targetState)
        {
            // Reset progress.
            bodyProgress.Value = 0;
            prevColor = bodyRenderer.color;
            prevState = targetState;
            targetState = state;
            targetColor = GetStateColor(state);
        }
        bodyProgress.Advance(Time.deltaTime * (targetState.fillState == LifeCellFillState.Empty ? 8 : 3), 1);

        var scale = ComputeBodyScale();
        var color = ComputeBodyColor();

        if (state.permanent) scale *= bgSize.Value;
        if (scale > 0)
        {
            body.transform.localScale = new(scale, scale, 1);
            bodyRenderer.color = color;
            body.gameObject.SetActive(true);
        }
        else body.gameObject.SetActive(false);

        return scale;
    }

    private void SyncCover(float a, float b)
    {
        float scale = Mathf.Max(a, b);
        cover.gameObject.SetActive(scale > 0);
        cover.transform.localScale = new(scale, scale, 1);
    }

    public void Sync(LifeCellState state)
    {
        var a = SyncBG(state);
        var b = SyncBody(state);
        SyncCover(a, b);

        // TODO: Particles
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
    private List<LifeCell> lifeCells = new();

    protected void Awake() => tracker.Init();

    protected void OnDestroy() => tracker.Unload();

    private void AddLifeCell(LifeCellState state)
    {
        var newCell = LifeCell.Create(lifeCells.Count == 0 ? null : lifeCells[lifeCells.Count - 1], lifeCells.Count, state);
        
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
