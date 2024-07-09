using TMPro;
using UnityEngine;

namespace Sein.Util;

internal class PlayerDataCache : MonoBehaviour
{
    public static PlayerDataCache Instance { get; private set; }

    public static void Hook()
    {
        GameObject obj = new("PlayerDataCache");
        DontDestroyOnLoad(obj);
        Instance = obj.AddComponent<PlayerDataCache>();
    }

    public bool HasLantern { get; private set; }
    public bool HivebloodEquipped { get; private set; }
    public bool Overcharmed { get; private set; }
    public bool Furied { get; private set; }

    private TextMeshPro? essenceTextMesh;
    public string EssenceText { get; private set; }

    private void Update()
    {
        var pd = PlayerData.instance;
        HasLantern = pd.GetBool(nameof(PlayerData.hasLantern));
        HivebloodEquipped = pd.GetBool(nameof(PlayerData.equippedCharm_29));
        Overcharmed = pd.GetBool(nameof(PlayerData.overcharmed));
        Furied = pd.GetBool(nameof(PlayerData.equippedCharm_6)) && pd.GetInt(nameof(PlayerData.health)) == 1 && (pd.GetInt(nameof(PlayerData.healthBlue)) < 1 || !pd.GetBool(nameof(PlayerData.equippedCharm_27)));

        essenceTextMesh ??= GOFinder.EssenceTextMesh();
        EssenceText = essenceTextMesh?.text ?? "";
    }
}
