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
        playSoundHook = HookOrig<HeroAudioController>(OverridePlaySound, "PlaySound", BindingFlags.Public | BindingFlags.Instance);

        On.HeroController.DoDoubleJump += SpawnDoubleJumpWave;
    }

    private static void OverrideDoDoubleJump(ILContext ctx)
    {
        ILCursor cursor = new(ctx);

        cursor.GotoNext(i => i.MatchCallvirt<ParticleSystem>("Play"));
        cursor.Remove();
        cursor.EmitDelegate(MaybePlayParticleSystem);
        cursor.GotoNext(MoveType.After, i => i.MatchLdfld<HeroController>("doubleJumpClip"));
        cursor.EmitDelegate(OverrideAudioClip(doubleJumps));
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
        cursor.EmitDelegate(OverrideAudioClip([sharpShadowDash]));
        cursor.GotoNext(MoveType.After, i => i.MatchLdfld<HeroController>("shadowDashClip"));
        cursor.EmitDelegate(OverrideAudioClip([shadowDash]));
    }

    private static void OverridePlaySound(ILContext ctx)
    {
        ILCursor cursor = new(ctx);

        cursor.GotoNext(MoveType.After, i => i.MatchLdfld<HeroAudioController>("dash"));
        cursor.EmitDelegate(OverrideAudioSource("dash", [dash]));
    }

    private static Func<AudioClip, AudioClip> OverrideAudioClip(List<AudioClip> replacements)
    {
        AudioClip Replace(AudioClip orig)
        {
            return SeinMod.OriActive() ? replacements[UnityEngine.Random.Range(0, replacements.Count)] : orig;
        }

        return Replace;

    }

    private static Dictionary<string, AudioClip> originals = new();

    private static Func<AudioSource, AudioSource> OverrideAudioSource(string name, List<AudioClip> replacements)
    {
        AudioSource Replace(AudioSource orig)
        {
            if (!originals.ContainsKey(name)) originals.Add(name, orig.clip);
            orig.clip = SeinMod.OriActive() ? replacements[UnityEngine.Random.Range(0, replacements.Count)] : originals[name];

            return orig;
        }

        return Replace;
    }

    private static ILHook HookOrig<T>(ILContext.Manipulator hook, string name, BindingFlags flags)
    {
        var method = typeof(T).GetMethod($"orig_{name}", flags) ?? typeof(T).GetMethod(name, flags);
        return new(method, hook);
    }
}
