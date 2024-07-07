using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Sein.Util;

static class GOFinder
{
    public static GameObject Knight() => GameManager.instance?.hero_ctrl?.gameObject ?? GameObject.Find("Knight");

    public static HeroController HeroController() => GameManager.instance?.hero_ctrl ?? GameObject.Find("Knight").GetComponent<HeroController>();

    public static GameObject HudCanvas() => GameObject.Find("_GameCameras/HudCamera/Hud Canvas");

    public static TextMeshPro? EssenceTextMesh() => GameObject.Find("_GameCameras/HudCamera/Hud Canvas/Extras/Dream Nail/Amount")?.GetComponent<TextMeshPro>();
}

static class GOExtensions
{
    public static IEnumerable<GameObject> AllChildren(this GameObject self)
    {
        for (int i = 0; i < self.transform.childCount; i++)
            yield return self.transform.GetChild(i).gameObject;
    }
}
