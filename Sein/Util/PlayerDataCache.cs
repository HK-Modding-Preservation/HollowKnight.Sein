using TMPro;
using UnityEngine;

namespace Sein.Util;

internal class PlayerDataCache : MonoBehaviour
{
    public static PlayerDataCache instance { get; private set; }

    public static void Hook()
    {
        GameObject obj = new("PlayerDataCache");
        DontDestroyOnLoad(obj);
        instance = obj.AddComponent<PlayerDataCache>();
    }

    public bool HivebloodEquipped { get; private set; }
    public bool Overcharmed { get; private set; }

    private TextMeshPro? essenceTextMesh;
    public string EssenceText { get; private set; }

    private void Update()
    {
        var pd = PlayerData.instance;
        HivebloodEquipped = pd.GetBool(nameof(PlayerData.equippedCharm_29));
        Overcharmed = pd.GetBool(nameof(PlayerData.overcharmed));

        essenceTextMesh ??= GOFinder.EssenceTextMesh();
        EssenceText = essenceTextMesh?.text ?? "";
    }
}
