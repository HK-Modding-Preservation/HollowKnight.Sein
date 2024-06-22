using CustomKnight;
using ItemChanger.Extensions;
using ItemChanger.FsmStateActions;
using UnityEngine;

namespace Sein;

internal class HivebloodWatcher
{
    public delegate void SkinToggled(bool on);
    public static event SkinToggled? OnSkinToggled;

    public static void Hook()
    {
        ItemChanger.Events.AddFsmEdit(new("Health", "Hive Health Regen"), WatchHiveblood);
    }

    public static bool HivebloodEquipped => PlayerData.instance.GetBool(nameof(PlayerData.equippedCharm_29));

    public static bool HivebloodHealing { get; private set; }

    private static void WatchHiveblood(PlayMakerFSM fsm)
    {
        fsm.GetState("Start Recovery").AddFirstAction(new Lambda(() => HivebloodHealing = true));

        foreach (string state in new string[] { "Init", "Idle", "Recover", "Cancel Recovery" })
            fsm.GetState(state).AddFirstAction(new Lambda(() => HivebloodHealing = false));
    }
}
