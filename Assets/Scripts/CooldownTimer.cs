using System;
using UnityEngine;

public class CooldownTimer
{
    private float? LastActionDone { get; set; }
    private float IntervalSeconds { get; set; }

    public CooldownTimer(float intervalSeconds)
    {
        IntervalSeconds = intervalSeconds;
    }

    public void UpdateActionTime()
    {
        LastActionDone = Time.time;
    }

    public bool CanWeDoAction()
    {
        var canWeDoAction = true;
        if (LastActionDone.HasValue)
        {
            var secondsSinceLastAction = Time.time - LastActionDone;
            canWeDoAction = secondsSinceLastAction > IntervalSeconds;
        }

        return canWeDoAction;
    }
}