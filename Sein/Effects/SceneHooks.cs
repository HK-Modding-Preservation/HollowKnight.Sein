using ItemChanger;

namespace Sein.Effects;

static class SceneHooks
{
    public delegate void ConditionalSceneHook(bool oriEnabled, SeinSettings settings);
    public delegate void SceneHook();

    public static void Hook(ConditionalSceneHook hook)
    {
        Events.OnSceneChange += _ => hook(SeinMod.OriActive(), SeinSettings.Instance);
    }
}
