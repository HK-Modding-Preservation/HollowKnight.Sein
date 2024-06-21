﻿using GlobalEnums;
using UnityEngine;

namespace Sein.Util;

internal abstract class AbstractParticleFactory<F, P> where F : AbstractParticleFactory<F, P> where P : AbstractParticle<F, P>
{
    private readonly ObjectPool<P> pool;

    public AbstractParticleFactory() => pool = new(Instantiate);

    protected abstract Sprite GetSprite();

    protected abstract string GetObjectName();

    protected virtual (GameObject, SpriteRenderer) CreateSprite()
    {
        GameObject obj = new(GetObjectName());
        obj.SetActive(false);

        var spriteRenderer = obj.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = GetSprite();

        return (obj, spriteRenderer);
    }

    protected virtual P Instantiate()
    {
        var (obj, spriteRenderer) = CreateSprite();

        var particle = obj.AddComponent<P>();
        particle.spriteRenderer = spriteRenderer;
        particle.pool = pool;

        return particle;
    }

    protected bool Launch(float prewarm, float lifetime, out P particle)
    {
        particle = pool.Lease();
        return particle.Reset(prewarm, lifetime);
    }
}

internal abstract class UIParticleFactory<F, P> : AbstractParticleFactory<F, P> where F: UIParticleFactory<F, P> where P : AbstractParticle<F, P>
{
    protected virtual int SortingOrder => 0;

    protected override (GameObject, SpriteRenderer) CreateSprite()
    {
        var (obj, spriteRenderer) = base.CreateSprite();
        obj.layer = (int)PhysLayers.UI;

        spriteRenderer.sortingLayerName = "Over";
        spriteRenderer.sortingOrder = SortingOrder;
        return (obj, spriteRenderer);
    }
}

internal abstract class AbstractParticle<F, P> : MonoBehaviour where F : AbstractParticleFactory<F, P> where P : AbstractParticle<F, P>
{
    internal SpriteRenderer spriteRenderer;
    internal ObjectPool<P> pool;

    private float age = 0;
    private float lifetime;

    protected float Progress => age / lifetime;

    protected float RProgress => 1 - Progress;

    protected float Age => age;

    protected float Lifetime => lifetime;

    protected float TimeRemaining => lifetime - age;

    protected abstract P Self();

    internal bool Reset(float prewarm, float lifetime)
    {
        if (prewarm >= lifetime)
        {
            pool.Return(Self());
            return false;
        }

        age = 0;
        this.lifetime = lifetime;

        gameObject.SetActive(true);
        Update(prewarm);
        return true;
    }

    protected virtual bool UseLocalPos => false;

    protected abstract Vector3 GetPos();

    protected abstract Vector3 GetScale();

    protected abstract float GetAlpha();

    private void Update() => Update(Time.deltaTime);

    protected virtual void Update(float time)
    {
        age += time;
        if (age >= Lifetime)
        {
            gameObject.SetActive(false);
            pool.Return(Self());
            return;
        }

        var pos = GetPos();
        if (UseLocalPos) transform.localPosition = pos;
        else transform.position = pos;

        transform.localScale = GetScale();
        spriteRenderer.SetAlpha(GetAlpha());
    }
}

internal abstract class LinearParticle<F, P> : AbstractParticle<F, P> where F : AbstractParticleFactory<F, P> where P : LinearParticle<F, P>
{
    private Vector3 posStart, posEnd;
    private Vector3 scaleStart, scaleEnd;
    private float alphaStart, alphaEnd;

    public void SetParams(Vector3 posStart, Vector3 posEnd, Vector3 scaleStart, Vector3 scaleEnd, float alphaStart, float alphaEnd)
    {
        this.posStart = posStart;
        this.posEnd = posEnd;
        this.scaleStart = scaleStart;
        this.scaleEnd = scaleEnd;
        this.alphaStart = alphaStart;
        this.alphaEnd = alphaEnd;
    }

    protected override float GetAlpha() => alphaStart + (alphaEnd - alphaStart) * Progress;

    protected override Vector3 GetPos() => posStart + (posEnd - posStart) * Progress;

    protected override Vector3 GetScale() => scaleStart + (scaleEnd - scaleStart) * Progress;
}