using Sein.Util;
using Sein.Watchers;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Sein.Hud;

internal class HudAttacher : PersistentAttacher<HudAttacher, Hud>
{
    protected override GameObject? FindParent() => GOFinder.HudCanvas();
}

internal class Hud : PersistentMonoBehaviour
{
    private static Vector3 LIVE_OFFSET = new(-7.65f, 6.75f, 0);
    private static Vector3 HIDE_OFFSET = new(-100, 0, 0);
    private static Vector3 SLIDE_OUT_OFFSET = new(0, 3.5f, 0);

    private List<GameObject> origChildren;
    private PlayMakerFSM slideOutFsm;
    private GameObject oriHud;

    private static float SLIDE_DURATION = 0.8f;
    private float outFraction = 1;

    protected void Awake()
    {
        origChildren = gameObject.AllChildren().ToList();
        slideOutFsm = gameObject.LocateMyFSM("Slide Out");

        oriHud = new("OriHud");
        DontDestroyOnLoad(oriHud);
        oriHud.transform.position = LIVE_OFFSET + HIDE_OFFSET + SLIDE_OUT_OFFSET;
        oriHud.layer = gameObject.layer;

        UpdateOriState(SkinWatcher.OriActive());
        SkinWatcher.OnSkinToggled += UpdateOriState;
        SeinSettings.OnSettingsChanged += SettingsChanged;

        // Local position center
        GameObject spiritLightHud = new("SpiritLightHud");
        spiritLightHud.transform.SetParent(oriHud.transform);
        spiritLightHud.transform.localPosition = Vector3.zero;
        spiritLightHud.AddComponent<SpiritLightHud>();

        GameObject essenceHud = new("EssenceHud");
        essenceHud.transform.SetParent(oriHud.transform);
        essenceHud.transform.localPosition = new(2.55f, -1.15f, 0);
        essenceHud.AddComponent<EssenceHud>();

        GameObject lifeHud = new("LifeHud");
        lifeHud.transform.SetParent(oriHud.transform);
        lifeHud.transform.localPosition = Vector3.zero;
        lifeHud.AddComponent<LifeHud>();

        GameObject energyHud = new("EnergyHud");
        energyHud.transform.SetParent(oriHud.transform);
        energyHud.transform.localPosition = Vector3.zero;
        energyHud.AddComponent<EnergyHud>();
    }

    protected override void OnDestroy()
    {
        SkinWatcher.OnSkinToggled -= UpdateOriState;
        SeinSettings.OnSettingsChanged -= SettingsChanged;

        Destroy(oriHud);
        base.OnDestroy();
    }

    private bool isOriActive = false;

    private void SettingsChanged(SeinSettings settings) => UpdateOriState(SkinWatcher.OriActive());

    private void UpdateOriState(bool oriActive)
    {
        oriActive &= SeinSettings.Instance.EnableHud;
        if (isOriActive == oriActive) return;

        isOriActive = oriActive;
        foreach (var go in origChildren) go.transform.localPosition += isOriActive ? HIDE_OFFSET : -HIDE_OFFSET;
        oriHud.transform.localPosition += isOriActive ? -HIDE_OFFSET : HIDE_OFFSET;
    }

    private bool IsIn()
    {
        var state = slideOutFsm.ActiveStateName;
        return state == "Idle" || state == "In" || state == "Come In";
    }

    private Vector3 GetOutOffset() => SLIDE_OUT_OFFSET * Mathf.Sqrt(outFraction);

    protected void Update()
    {
        var isIn = IsIn();
        var oldOffset = GetOutOffset();
        if (isIn && outFraction > 0)
        {
            outFraction -= Time.deltaTime / SLIDE_DURATION;
            if (outFraction < 0) outFraction = 0;
        }
        else if (!isIn && outFraction < 1)
        {
            outFraction += Time.deltaTime / SLIDE_DURATION;
            if (outFraction > 1) outFraction = 1;
        }
        var newOffset = GetOutOffset();
        oriHud.transform.localPosition += newOffset - oldOffset;
    }
}
