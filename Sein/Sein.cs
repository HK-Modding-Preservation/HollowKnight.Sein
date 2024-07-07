using Modding;
using PurenailCore.ModUtil;
using Sein.Effects;
using Sein.Util;
using Sein.Watchers;
using System;

namespace Sein;

public class SeinMod : Mod
{
    private static SeinMod? _instance;

    internal static SeinMod Instance
    {
        get
        {
            if (_instance == null)
                throw new InvalidOperationException($"An instance of {nameof(SeinMod)} was never constructed");
            return _instance;
        }
    }

    private static readonly string _version = VersionUtil.ComputeVersion<SeinMod>();

    public override string GetVersion() => _version;

    public SeinMod() : base("Sein")
    {
        _instance = this;
    }

    public static bool OriActive() => SkinWatcher.OriActive();

    public override void Initialize()
    {
        EssenceWatcher.Hook();
        HivebloodWatcher.Hook();
        Hud.HudAttacher.Hook();
        ILHooks.Hook();
        Orb.Hook();
        PlayerDataCache.Hook();
        Regenerate.Hook();
        SkinWatcher.Hook();
    }
}
