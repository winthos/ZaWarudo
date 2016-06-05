using UnityEngine;
using System.Collections;

public class ExBullet : FFComponent {

    FFAction.ActionSequence moveSequence;
    FFAction.ActionSequence deathSequence;

    public float BulletSpeed = 500.0f;
    public float TimeToLive = 10.0f;

    public void FireBullet(double time, FFVector3 position)
    {
        // Fire
        transform.position = position;
        moveSequence = action.Sequence();
        moveSequence.TimeWarpFrom(time);
        FireSequenceCall();

        // Death
        deathSequence = action.Sequence();
        deathSequence.Delay(TimeToLive);
        deathSequence.Sync();
        deathSequence.Call(DestroyBullet);
    }

    private void FireSequenceCall()
    {
        const float callTime = 3.0f;
        moveSequence.Property(ffposition, ffposition + (new Vector3(0, 1, 0) * BulletSpeed * callTime), FFEase.E_Continuous, callTime);
        moveSequence.Sync();
        moveSequence.Call(FireSequenceCall);
    }

    public void DestroyBullet()
    {
        GameObject.Destroy(gameObject);
    }
}
