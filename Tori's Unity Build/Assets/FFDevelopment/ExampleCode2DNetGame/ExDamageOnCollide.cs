using UnityEngine;
using System.Collections;
using System;

[Flags]
public enum DamageGroup
{
    None = 0,
    PlayerShips = 1,
    Enemies = 2,
    Debris = 4,
}

public class ExDamageOnCollide : MonoBehaviour {

    [Range(1, 15)]
    public int Damage = 1;
    public bool DestroyOnCollision = true;
    public DamageGroup[] CanDamage;
    private DamageGroup DamageGroup;


    void Awake()
    {
        foreach (var group in CanDamage)
        {
            DamageGroup |= group;
        }
    }

    void OnCollisionEnter2D(Collision2D coll)
    {
        if(FFServer.isLocal)
        {
            if (Damage > 0)
            {
                // Send Messages
                ExHealth.ApplyHealthEvent e;
                e.amount = -Damage;
                e.time = (float)FFSystem.time;
                e.group = DamageGroup;

                FFMessageBoard<ExHealth.ApplyHealthEvent>.SendToLocalUp(e, coll.gameObject);
                FFMessageBoard<ExHealth.ApplyHealthEvent>.SendToNetUp(e, coll.gameObject, true);

                if (DestroyOnCollision)
                    gameObject.Destroy();
            }
        }

    }
	
	// Update is called once per frame
	void Update () {
	
	}
}
