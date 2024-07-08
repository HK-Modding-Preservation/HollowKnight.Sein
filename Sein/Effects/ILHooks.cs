using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Sein.Effects;

internal static class ILHooks
{
    private static Assembly asm = typeof(ILHooks).Assembly;

    private static AudioClip dash = LoadAudioClip("dash");
    private static List<AudioClip> doubleJumps = [LoadAudioClip("doubleJumpA"), LoadAudioClip("doubleJumpB")];
    private static List<AudioClip> geoCollects = [LoadAudioClip("geoCollectA"), LoadAudioClip("geoCollectB"), LoadAudioClip("geoCollectC")];
    private static AudioClip shadowDash = LoadAudioClip("shadowDash");
    private static AudioClip sharpShadowDash = LoadAudioClip("sharpShadowDash");

    private static AudioClip LoadAudioClip(string name)
    {
        using Stream s = asm.GetManifestResourceStream($"Sein.Resources.Sounds.{name}.wav");
        return SFCore.Utils.WavUtils.ToAudioClip(s, name);
    }

    private static ILHook doDoubleJumpHook;
    private static ILHook heroDashHook;
    private static ILHook playSoundHook;

    public static void Hook()
    {
        doDoubleJumpHook = HookOrig<HeroController>(OverrideDoDoubleJump, "DoDoubleJump", BindingFlags.NonPublic | BindingFlags.Instance);
        heroDashHook = HookOrig<HeroController>(OverrideHeroDash, "HeroDash", BindingFlags.NonPublic | BindingFlags.Instance);
        playSoundHook = HookOrig<HeroAudioController>(OverridePlayMovementSound, "PlaySound", BindingFlags.Public | BindingFlags.Instance);

        On.GeoControl.OnEnable += ChangeGeoSounds;
        On.HeroController.DoDoubleJump += SpawnDoubleJumpWave;
    }

    private static void OverrideDoDoubleJump(ILContext ctx)
    {
        ILCursor cursor = new(ctx);

        cursor.GotoNext(i => i.MatchCallvirt<ParticleSystem>("Play"));
        cursor.Remove();
        cursor.EmitDelegate(MaybePlayParticleSystem);
        cursor.GotoNext(MoveType.After, i => i.MatchLdfld<HeroController>("doubleJumpClip"));
        cursor.EmitDelegate(OverrideMovementAudioClip(doubleJumps));
    }

    private static void MaybePlayParticleSystem(ParticleSystem sys)
    {
        if (SeinMod.OriActive()) return;
        sys.Play();
    }

    private static void SpawnDoubleJumpWave(On.HeroController.orig_DoDoubleJump orig, HeroController self)
    {
        orig(self);
        if (!SeinMod.OriActive()) return;

        var r2d = self.gameObject.GetComponent<Rigidbody2D>();
        DoubleJumpWave.Spawn(self.gameObject.transform.position, r2d.velocity.x);
    }

    private static void OverrideHeroDash(ILContext ctx)
    {
        ILCursor cursor = new(ctx);

        cursor.GotoNext(MoveType.After, i => i.MatchLdfld<HeroController>("sharpShadowClip"));
        cursor.EmitDelegate(OverrideMovementAudioClip([sharpShadowDash]));
        cursor.GotoNext(MoveType.After, i => i.MatchLdfld<HeroController>("shadowDashClip"));
        cursor.EmitDelegate(OverrideMovementAudioClip([shadowDash]));
    }

    private static void OverridePlayMovementSound(ILContext ctx)
    {
        ILCursor cursor = new(ctx);

        cursor.GotoNext(MoveType.After, i => i.MatchLdfld<HeroAudioController>("dash"));
        cursor.EmitDelegate(OverrideMovementAudioSource("dash", [dash]));
    }

    private static Func<AudioClip, AudioClip> OverrideMovementAudioClip(List<AudioClip> replacements)
    {
        AudioClip Replace(AudioClip orig)
        {
            return (SeinMod.OriActive() && SeinSettings.Instance.EnableMovementSounds) ? replacements[UnityEngine.Random.Range(0, replacements.Count)] : orig;
        }

        return Replace;

    }

    private static Dictionary<string, AudioClip> origAudioClips = new();

    private static Func<AudioSource, AudioSource> OverrideMovementAudioSource(string name, List<AudioClip> replacements)
    {
        AudioSource Replace(AudioSource orig)
        {
            if (!origAudioClips.ContainsKey(name)) origAudioClips.Add(name, orig.clip);
            orig.clip = (SeinMod.OriActive() && SeinSettings.Instance.EnableMovementSounds) ? replacements[UnityEngine.Random.Range(0, replacements.Count)] : origAudioClips[name];

            return orig;
        }

        return Replace;
    }

    private static Dictionary<string, AudioClip[]> origAudioClipArrays = new();

    private static void ChangeGeoSounds(On.GeoControl.orig_OnEnable orig, GeoControl self)
    {
        if (!origAudioClipArrays.ContainsKey("geo")) origAudioClipArrays.Add("geo", self.pickupSounds);
        self.pickupSounds = (SeinMod.OriActive() && SeinSettings.Instance.EnableGeoSounds) ? geoCollects.ToArray() : origAudioClipArrays["geo"];

        orig(self);
    }

    private static ILHook HookOrig<T>(ILContext.Manipulator hook, string name, BindingFlags flags)
    {
        var method = typeof(T).GetMethod($"orig_{name}", flags) ?? typeof(T).GetMethod(name, flags);
        return new(method, hook);
    }
}
