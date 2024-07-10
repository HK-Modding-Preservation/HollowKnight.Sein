using CustomKnight;
using CustomKnightSuperAnimationAddon;
using ItemChanger.Internal.Menu;
using Modding;
using PurenailCore.ModUtil;
using Sein.Effects;
using Sein.Util;
using Sein.Watchers;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sein;

public record SeinSettings
{
    public static event Action<SeinSettings> OnSettingsChanged;
    public static void UpdateSettings() => OnSettingsChanged?.Invoke(Instance);

    public static SeinSettings Instance => SeinMod.GS;

    public string SkinDisplayName = "Ori";
    public bool EnableHud = true;
    public bool EnableSein = true;
    public bool EnableGeoSounds = true;
    public bool EnableMovementSounds = true;
}

public class OriSkin : ISelectableSkin
{
    public static OriSkin Instance { get; private set; } = new();

    private Dictionary<string, byte[]> files = new();
    private Dictionary<string, Texture2D> textures = new();

    private const string SkinResourcePrefix = "Sein.Resources.Skin.";

    private static string FixLocalName(string name)
    {
        var parts = name.Split('.');

        List<string> fixedParts = new();
        for (int i = 0; i < parts.Length - 2; i++) fixedParts.Add(parts[i].Replace('_', ' '));
        fixedParts.Add(parts[parts.Length - 2]);

        return $"{string.Join("/", fixedParts)}.{parts[parts.Length - 1]}";
    }

    private OriSkin()
    {
        var asm = typeof(OriSkin).Assembly;

        foreach (var name in asm.GetManifestResourceNames())
        {
            if (!name.StartsWith(SkinResourcePrefix)) continue;

            var localName = FixLocalName(name.Substring(SkinResourcePrefix.Length));

            if (localName.EndsWith(".png")) textures[localName] = Satchel.AssemblyUtils.GetTextureFromResources(name);
            files[localName] = Satchel.AssemblyUtils.GetBytesFromResources(name);
        }
    }


    public bool Exists(string FileName) => files.ContainsKey(FileName) || textures.ContainsKey(FileName);

    public string GetCinematicUrl(string CinematicName) => "";

    public byte[] GetFile(string FileName) => files[FileName];

    public string GetId() => "SeinMod-Ori";

    public string GetName() => SeinSettings.Instance.SkinDisplayName;

    public string getSwapperPath() => "";

    public Texture2D GetTexture(string FileName) => textures[FileName];

    public bool HasCinematic(string CinematicName) => false;

    public bool hasSwapper() => false;

    public bool shouldCache() => true;
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

        CustomKnight.CustomKnight.OnInit += (_, _) => SkinManager.AddSkin(OriSkin.Instance);
        SkinManager.OnSetSkin += (_, _) => SetOriEnabled();
    }

    public static new void Log(string msg) => (Instance as Modding.ILogger).Log(msg);

    public static event Action? OnSkinChanged;

    private static void SetOriEnabled()
    {
        var prev = OriEnabled;
        OriEnabled = SkinManager.GetCurrentSkin() == OriSkin.Instance;
        if (OriEnabled != prev) OnSkinChanged?.Invoke();
    }

    private static bool OriEnabled = false;

    public static bool OriActive() => OriEnabled;

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

        SkinManager.AddSkin(OriSkin.Instance);
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
