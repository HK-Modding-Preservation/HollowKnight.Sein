using ItemChanger.Extensions;
using ItemChanger.FsmStateActions;

namespace Sein.Watchers;

internal class EssenceWatcher
{
    public static void Hook() => ItemChanger.Events.AddFsmEdit(new("Amount", "FSM"), WatchEssenceText);

    public static bool EssenceUp { get; private set; }

    private static void WatchEssenceText(PlayMakerFSM fsm)
    {
        fsm.GetState("Up")?.AddFirstAction(new Lambda(() => EssenceUp = true));
        fsm.GetState("Upped")?.AddFirstAction(new Lambda(() => EssenceUp = true));
        fsm.GetState("Downed")?.AddFirstAction(new Lambda(() => EssenceUp = false));
    }
}
