using CustomKnight;
using UnityEngine;

namespace Sein.Watchers;

// TODO: There should be a hook for this in CustomKnight
internal class SkinWatcher : MonoBehaviour
{
    public delegate void SkinToggled(bool on);
    public static event SkinToggled? OnSkinToggled;

    public static void Hook()
    {
        var watcher = new GameObject("Skin Watcher");
        watcher.AddComponent<SkinWatcher>();
        DontDestroyOnLoad(watcher);
    }

    private static bool oriActive = false;

    public static bool OriActive() => oriActive;

    protected void Update()
    {
        bool oriActiveNow = SkinManager.GetCurrentSkin().GetId() == "Ori";
        if (oriActiveNow != oriActive)
        {
            oriActive = oriActiveNow;
            OnSkinToggled?.Invoke(oriActiveNow);
        }
    }
}
