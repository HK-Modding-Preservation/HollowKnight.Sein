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

internal class LifeCell : AbstractUICell<LifeCell, LifeCellState>
{
    private static IC.EmbeddedSprite firstCover = new("lifecell1cover");
    private static IC.EmbeddedSprite otherCover = new("lifecellncover");

    protected override LifeCellState DefaultState() => new() { fillState = LifeCellFillState.Empty };

    protected override Sprite GetCoverSprite(int index) => index == 0 ? firstCover.Value : otherCover.Value;

    protected override float BodyAdvanceSpeed() => targetState.fillState == LifeCellFillState.Filled ? 3 : 8;

    protected override bool StateIsPermanent(LifeCellState state) => state.permanent;

    private static readonly Color FURY_COLOR = Hex(170, 44, 22);
    private static readonly Color LIFEBLOOD_COLOR = Hex(93, 183, 209);
    private static readonly Color HIVEBLOOD_COLOR = Hex(245, 153, 52);
    private static readonly Color LIFE_COLOR = Hex(201, 233, 97);

    protected override Color GetBodyColor(LifeCellState state)
    {
        if (state.furied) return FURY_COLOR;
        else if (state.lifeblood) return LIFEBLOOD_COLOR;
        else if (state.hiveblood) return HIVEBLOOD_COLOR;
        else return LIFE_COLOR;
    }

    protected override Color GetFrameColor(LifeCellState state)
    {
        if (state.furied) return FURY_COLOR.Darker(0.45f);
        else if (state.hiveblood) return HIVEBLOOD_COLOR.Darker(state.lifeblood ? 0.25f : 0.55f);
        else if (state.lifeblood) return LIFEBLOOD_COLOR.Darker(0.35f);
        else return LIFE_COLOR.Darker(0.45f);
    }

    protected override float ComputeBodyScale(LifeCellState state) => state.fillState == LifeCellFillState.Filled ? 1 : 0;

    private const float HEAL_PARTICLES_PER_SEC = 100;
    private const float HEAL_PARTICLES_TIME = 0.3f;
    private const float DAMAGE_PARTICLES_PER_SEC = 150;
    private const float DAMAGE_PARTICLES_TIME = 0.2f;
    private const float LIFEBLOOD_DRIP_PER_SEC = 6.5f;
    private const float LIFEBLOOD_DRIP_TIME = 1.85f;
    private const float HIVEBLOOD_DRIP_PER_SEC = 13f;
    private const float HIVEBLOOD_DRIP_TIME = 1.25f;

    private PeriodicFloatTicker healTicker = RatedTicker(HEAL_PARTICLES_PER_SEC);
    private PeriodicFloatTicker damageTicker = RatedTicker(DAMAGE_PARTICLES_PER_SEC);
    private PeriodicFloatTicker lifebloodDripTicker = RatedTicker(LIFEBLOOD_DRIP_PER_SEC);
    private PeriodicFloatTicker hivebloodDripTicker = RatedTicker(HIVEBLOOD_DRIP_PER_SEC);

    protected override void EmitParticles(float bodySize)
    {
        if (bodySize > 0 && bodySize < 1)
        {
            if (targetState.fillState == LifeCellFillState.Filled)
                TickParticles(healTicker, Time.deltaTime, HEAL_PARTICLES_TIME, targetBodyColor.Value, UICellParticleMode.Inwards);
            else
                TickParticles(damageTicker, Time.deltaTime, DAMAGE_PARTICLES_TIME, prevBodyColor, UICellParticleMode.Outwards);
        }
        else
        {
            if (targetState.lifeblood) TickParticles(lifebloodDripTicker, Time.deltaTime, LIFEBLOOD_DRIP_TIME, LIFEBLOOD_COLOR, UICellParticleMode.Drip);
            if (targetState.fillState == LifeCellFillState.Healing) TickParticles(hivebloodDripTicker, Time.deltaTime, HIVEBLOOD_DRIP_TIME, HIVEBLOOD_COLOR, UICellParticleMode.Drip);
        }
    }
}

internal class LifeHud : AbstractCellHud<LifeCell, LifeCellState>
{
    protected override int OffsetSign() => 1;

    protected override LifeCellState EmptyCellState() => new() { fillState = LifeCellFillState.Empty };

    protected override List<LifeCellState> GetCellStates()
    {
        var pd = PlayerData.instance;

        var health = pd.GetInt(nameof(PlayerData.health));
        var maxHealth = pd.GetInt(nameof(PlayerData.maxHealth));
        var healthBlue = pd.GetInt(nameof(PlayerData.healthBlue));
        bool hiveblood = HivebloodWatcher.HivebloodEquipped;
        bool hivebloodHealing = HivebloodWatcher.HivebloodHealing;
        bool jonis = pd.GetBool(nameof(PlayerData.equippedCharm_27));
        bool furied = false;  // TODO: Handle fury

        List<LifeCellState> cellStates = new();
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

        return cellStates;
    }
}
