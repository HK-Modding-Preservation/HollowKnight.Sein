using ItemChanger.Internal.Menu;
using Modding;
using PurenailCore.ModUtil;
using Sein.Effects;
using Sein.Util;
using Sein.Watchers;
using System;

namespace Sein;

public record SeinSettings
{
    public static event Action<SeinSettings> OnSettingsChanged;
    public static void UpdateSettings() => OnSettingsChanged?.Invoke(Instance);

    public static SeinSettings Instance => SeinMod.GS;

    public bool EnableHud = true;
    public bool EnableSein = true;
    public bool EnableGeoSounds = true;
    public bool EnableMovementSounds = true;
}

public class SeinMod : Mod, IGlobalSettings<SeinSettings>, ICustomMenuMod
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
        Flash.Hook();
        HivebloodWatcher.Hook();
        Hud.HudAttacher.Hook();
        ILHooks.Hook();
        Orb.Hook();
        PlayerDataCache.Hook();
        Regenerate.Hook();
        SkinWatcher.Hook();
    }

    public static SeinSettings GS { get; private set; } = new();

    public void OnLoadGlobal(SeinSettings s) => GS = s;

    public SeinSettings OnSaveGlobal() => GS;

    public bool ToggleButtonInsideMenu => false;

    public MenuScreen GetMenuScreen(MenuScreen modListMenu, ModToggleDelegates? toggleDelegates)
    {
        ModMenuScreenBuilder builder = new("Sein", modListMenu);
        builder.AddHorizontalOption(new()
        {
            Name = "Enable Ori Hud",
            Description = "Use customized hud from the Ori games",
            Values = ["No", "Yes"],
            Saver = SaveBool(b => GS.EnableHud = b),
            Loader = LoadBool(() => GS.EnableHud),
        });
        builder.AddHorizontalOption(new()
        {
            Name = "Enable Sein",
            Description = "Include animated Sein companion with Ori skin",
            Values = ["No", "Yes"],
            Saver = SaveBool(b => GS.EnableSein = b),
            Loader = LoadBool(() => GS.EnableSein),
        });
        builder.AddHorizontalOption(new()
        {
            Name = "Enable Geo Sounds",
            Description = "Use sfx from Ori for 'spirit light' (geo) collection",
            Values = ["No", "Yes"],
            Saver = SaveBool(b => GS.EnableGeoSounds = b),
            Loader = LoadBool(() => GS.EnableGeoSounds),
        });
        builder.AddHorizontalOption(new()
        {
            Name = "Enable Movement Sounds",
            Description = "Use sfx from Ori for Dash and 'Double Jump' (Monarch Wings) movement",
            Values = ["No", "Yes"],
            Saver = SaveBool(b => GS.EnableMovementSounds = b),
            Loader = LoadBool(() => GS.EnableMovementSounds),
        });
        return builder.CreateMenuScreen();
    }

    private Action<int> SaveBool(Action<bool> saver)
    {
        return i =>
        {
            saver(i == 1);
            SeinSettings.UpdateSettings();
        };
    }

    private Func<int> LoadBool(Func<bool> loader) => () => loader() ? 1 : 0;
}
