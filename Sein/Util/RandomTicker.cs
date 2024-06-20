using System.Collections.Generic;
using UnityEngine;

namespace Sein.Util;

internal class RandomTicker
{
    private readonly int min;
    private readonly int max;
    private int next;

    public RandomTicker(int min, int max)
    {
        this.min = min;
        this.max = max;
        this.next = UnityEngine.Random.Range(1, UnityEngine.Random.Range(min, max + 1) + 1);
    }

    // Each returned int represents the number of ticks that passed between the start of this call and the next event.
    // So for a non-random ticker that always takes 5 ticks per event, Tick(3) would yield nothing, a subsequent Tick(21) would yield [2, 7, 12, 17], and a tertiary Tick(5) would yield [1].
    public IEnumerable<int> Tick(int ticks)
    {
        while (next <= ticks)
        {
            yield return next;
            ticks -= next;
            next = UnityEngine.Random.Range(min, max + 1);
        }

        next -= ticks;
    }
}

internal class PeriodicFloatTicker
{
    private readonly RandomTicker ticker;
    private readonly int intPeriod;
    private readonly float floatPeriod;

    public PeriodicFloatTicker(float period, int ticks, int minTicks, int maxTicks)
    {
        ticker = new(minTicks, maxTicks);
        this.intPeriod = ticks;
        this.floatPeriod = period;
    }

    private int prevInt = 0;

    public IEnumerable<float> TickFloats(float ticks)
    {
        var prevFloat = prevInt * floatPeriod / intPeriod;
        var newInt = Mathf.FloorToInt((prevFloat + ticks) * intPeriod / floatPeriod);

        float consumed = 0;
        var tick = prevInt;
        foreach (var next in ticker.Tick(newInt - prevInt))
        {
            consumed += next * floatPeriod / intPeriod;
            tick += next;
            tick %= intPeriod;
            yield return consumed;
        }

        prevInt = newInt % intPeriod;
    }
}
