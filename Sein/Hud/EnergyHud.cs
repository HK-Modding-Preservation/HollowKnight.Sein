using System.Collections.Generic;

namespace Sein.Hud;

internal record EnergyCellState
{
    public int energy;
    public bool locked;
}

internal class EnergyState
{
    public List<EnergyCellState> cellStates;
}

