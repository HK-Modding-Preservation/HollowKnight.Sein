using UnityEngine;

namespace Sein.Effects;

internal class DoubleJumpWave : MonoBehaviour
{
    private static IC.EmbeddedSprite WaveSprite = new("djumpwave");

    private static float SPEED = 19;
    private static float ANGLE_BASE = 17;
    private static float SPAWN_OFFSET = 1.1f;
    private static float LIFETIME = 0.225f;
    private static float SCALE_MULT = 0.65f;
    private static float SCALE_START = 0.75f;
    private static float SCALE_END = 2.5f;
    private static float ALPHA_START = 0.75f;
    private static float ALPHA_END = 0f;

    public static void Spawn(Vector3 pos, float xvel)
    {
        var angle = ComputeAngle(xvel);

        GameObject obj = new("DoubleJumpWave");
        obj.SetActive(false);

        var velocity = Quaternion.Euler(0, 0, angle - 90) * new Vector3(SPEED, 0, 0);
        obj.transform.position = pos + velocity.normalized * SPAWN_OFFSET;
        obj.transform.localRotation = Quaternion.Euler(0, 0, angle);
        obj.transform.localScale = new(SCALE_MULT, SCALE_MULT * SCALE_START, 1);

        var spriteRenderer = obj.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = WaveSprite.Value;

        var wave = obj.AddComponent<DoubleJumpWave>();
        wave.velocity = velocity;
        wave.spriteRenderer = spriteRenderer;

        obj.SetActive(true);
    }

    private static float ComputeAngle(float xvel)
    {
        if (Mathf.Abs(xvel) < 0.1f) return 0;
        else if (xvel > 0) return -ANGLE_BASE;
        else return ANGLE_BASE;
    }

    private Vector3 velocity;
    private SpriteRenderer spriteRenderer;
    private float age = 0;

    private void Update()
    {
        age += Time.deltaTime;
        float prog = age / LIFETIME;
        if (prog > 1)
        {
            Destroy(gameObject);
            return;
        }

        transform.position += velocity * Time.deltaTime;

        float scale = SCALE_START + (SCALE_END - SCALE_START) * prog;
        transform.localScale = new(SCALE_MULT, SCALE_MULT * scale, 1);

        float alpha = ALPHA_START + (ALPHA_END - ALPHA_START) * prog;
        var c = spriteRenderer.color;
        spriteRenderer.color = new(c.r, c.g, c.b, alpha);
    }
}
