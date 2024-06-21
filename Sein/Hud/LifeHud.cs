using System.Collections.Generic;

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
    public bool hiveblood;
    public bool lifeblood;
    public bool furied;
}

internal class LifeState
{
    public List<LifeCellState> cellStates = new();
}
