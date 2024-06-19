using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Sein.Effects;

internal static class Sounds
{
    private static Assembly asm = typeof(Sounds).Assembly;
    private static AudioClip dash = LoadAudioClip("dash");
    private static AudioClip shadowDash = LoadAudioClip("shadowDash");
    private static AudioClip sharpShadowDash = LoadAudioClip("sharpShadowDash");

    private static AudioClip LoadAudioClip(string name)
    {
        using Stream s = asm.GetManifestResourceStream($"Sein.Resources.Sounds.{name}.wav");
        return SFCore.Utils.WavUtils.ToAudioClip(s, name);
    }

    private static ILHook heroDashHook;
    private static ILHook playSoundHook;

    public static void Hook()
    {
        heroDashHook = HookOrig<HeroController>(OverrideHeroDash, "HeroDash", BindingFlags.NonPublic | BindingFlags.Instance);
        playSoundHook = HookOrig<HeroAudioController>(OverridePlaySound, "PlaySound", BindingFlags.Public | BindingFlags.Instance);
    }

    private static void OverrideHeroDash(ILContext ctx)
    {
        ILCursor cursor = new(ctx);

        cursor.GotoNext(MoveType.After, i => i.MatchLdfld<HeroController>("sharpShadowClip"));
        cursor.EmitDelegate(OverrideAudioClip(sharpShadowDash));
        cursor.GotoNext(MoveType.After, i => i.MatchLdfld<HeroController>("shadowDashClip"));
        cursor.EmitDelegate(OverrideAudioClip(shadowDash));
    }

    private static void OverridePlaySound(ILContext ctx)
    {
        ILCursor cursor = new(ctx);

        cursor.GotoNext(MoveType.After, i => i.MatchLdfld<HeroAudioController>("dash"));
        cursor.EmitDelegate(OverrideAudioSource("dash", dash));
    }

    private static Func<AudioClip, AudioClip> OverrideAudioClip(AudioClip replacement)
    {
        AudioClip Replace(AudioClip orig)
        {
            return SeinMod.OriActive() ? replacement : orig;
        }

        return Replace;
    }

    private static Dictionary<string, AudioClip> originals = new();

    private static Func<AudioSource, AudioSource> OverrideAudioSource(string name, AudioClip replacement)
    {
        AudioSource Replace(AudioSource orig)
        {
            if (!originals.ContainsKey(name)) originals.Add(name, orig.clip);
            orig.clip = SeinMod.OriActive() ? replacement : originals[name];

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
