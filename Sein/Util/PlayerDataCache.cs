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

    private TextMeshPro? essenceTextMesh;
    public string EssenceText { get; private set; }

    private void Update()
    {
        var pd = PlayerData.instance;
        HasLantern = pd.GetBool(nameof(PlayerData.hasLantern));
        HivebloodEquipped = pd.GetBool(nameof(PlayerData.equippedCharm_29));
        Overcharmed = pd.GetBool(nameof(PlayerData.overcharmed));

        essenceTextMesh ??= GOFinder.EssenceTextMesh();
        EssenceText = essenceTextMesh?.text ?? "";
    }
}
