﻿using Sein.Util;
using System.Collections.Generic;
using UnityEngine;

namespace Sein.Hud;

internal enum EnergyCellFillState
{
    Active,
    Locked,
    Shattered,
}

internal record EnergyCellState
{
    public int energy;  // Only relevant for Active or Locked
    public EnergyCellFillState fillState;
}

internal class EnergyCell : AbstractUICell<EnergyCell, EnergyCellState>
{
    protected override float BodyAdvanceSpeed() => 1.5f;

    protected override float ComputeBodyScale(EnergyCellState state)
    {
        if (state.fillState == EnergyCellFillState.Shattered) return 1;
        else return Mathf.Sqrt(state.energy / 33f);
    }

    protected override EnergyCellState DefaultState() => new();

    private static float SOUL_DRIP_RATE = 9f;
    private static float SOUL_DRIP_TIME = 1.85f;
    private static float SOUL_UNLOCK_RATE = 135;
    private static float SOUL_UNLOCK_TIME = 0.6f;

    private PeriodicFloatTicker soulTicker = RatedTicker(SOUL_DRIP_RATE);
    private PeriodicFloatTicker unlockTicker = RatedTicker(SOUL_UNLOCK_RATE);

    protected override void EmitParticles(float bodySize)
    {
        if (targetState.energy > 0 && targetState.fillState == EnergyCellFillState.Active) TickParticles(soulTicker, Time.deltaTime * targetState.energy / 33, SOUL_DRIP_TIME, SOUL_COLOR, UICellParticleMode.Drip);
        if (prevState.fillState != EnergyCellFillState.Active && targetState.fillState == EnergyCellFillState.Active)
            TickParticles(unlockTicker, Time.deltaTime, SOUL_UNLOCK_TIME, SOUL_COLOR, UICellParticleMode.Outwards);
    }

    private static Color SOUL_COLOR = Hex(172, 195, 255);
    private static Color LOCKED_SOUL_COLOR = SOUL_COLOR.Darker(0.45f);
    private static Color DEAD_SOUL_COLOR = Hex(40, 58, 140);

    protected override Color GetBodyColor(EnergyCellState state)
    {
        if (state.fillState == EnergyCellFillState.Shattered) return DEAD_SOUL_COLOR;
        else if (state.fillState == EnergyCellFillState.Locked) return LOCKED_SOUL_COLOR;
        else return SOUL_COLOR.Darker((previousCell == null && state.energy < 33) ? 0.25f : 0);
    }

    private static IC.EmbeddedSprite firstCover = new("energycell1cover");
    private static IC.EmbeddedSprite otherCover = new("energycellncover");

    protected override Sprite GetCoverSprite(int index) => index == 0 ? firstCover.Value : otherCover.Value;

    protected override Color GetFrameColor(EnergyCellState state) => GetBodyColor(state).Darker(0.25f);

    protected override bool StateIsPermanent(EnergyCellState state) => true;
}

internal class EnergyHud : AbstractCellHud<EnergyCell, EnergyCellState>
{
    protected override int OffsetSign() => -1;

    protected override EnergyCellState EmptyCellState() => new();

    private IEnumerable<int> SplitEnergy(int energy)
    {
        while (energy > 0)
        {
            int toUse = Mathf.Min(energy, 33);
            yield return toUse;
            energy -= toUse;
        }
    }

    protected override List<EnergyCellState> GetCellStates()
    {
        var pd = PlayerData.instance;

        var mpCharge = pd.GetInt(nameof(PlayerData.MPCharge));
        var mpReserve = pd.GetInt(nameof(PlayerData.MPReserve));
        var soulLimited = pd.GetBool(nameof(PlayerData.soulLimited));
        var mpReserveMax = pd.GetInt(nameof(PlayerData.MPReserveMax));

        List<EnergyCellState> cellStates = new();

        if (soulLimited) cellStates.Add(new() { fillState = EnergyCellFillState.Shattered });
        foreach (var used in SplitEnergy(mpReserve))
        {
            cellStates.Add(new()
            {
                energy = used,
                fillState = EnergyCellFillState.Locked,
            });
        }
        foreach (var used in SplitEnergy(mpCharge))
        {
            cellStates.Add(new()
            {
                energy = used,
                fillState = EnergyCellFillState.Active,
            });
        }

        int maxCells = 3 + mpReserveMax / 33;
        while (cellStates.Count < maxCells) cellStates.Add(new() { fillState = EnergyCellFillState.Active });

        return cellStates;
    }
}

