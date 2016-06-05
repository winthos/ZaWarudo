using UnityEngine;
using System.Collections;
using System;


public class ExHealth : FFComponent
{
    // Events
    [Serializable]
    public struct ApplyHealthEvent
    {
        public float time;
        public int amount;
        public DamageGroup group;
    }

    public struct EmptyHealthEvent
    {
        public int currentHealth;
    }

    [Range(0, 15)]
    public int maxHealth;
    private int currentHealth;
    public DamageGroup[] damageGroups;
    private DamageGroup damageGroup;

    private FFAction.ActionSequence deathSequence;

    void Awake()
    {
        foreach (var group in damageGroups)
        {
            damageGroup |= group;
        }

        FFSystem.RegisterNetGameObject(gameObject, gameObject.name);
        FFMessageBoard<ApplyHealthEvent>.Connect(OnApplyHealth, gameObject);
        FFMessageBoard<EmptyHealthEvent>.Connect(OnHealthEmpty, gameObject);

        if(FFServer.isLocal == false)
        {
            FFMessageBoard<ExHealth>.Connect(OnServerUpdate, gameObject);
        }
        else
        {
            FFMessage<FFNetEvents.ClientConnectedEvent>.Connect(OnClientConnected);
        }
    }

    private void OnClientConnected(FFNetEvents.ClientConnectedEvent e)
    {
        FFMessageBoard<ExHealth>.SendToNet((ExHealth)this.MemberwiseClone(), gameObject, true);
    }
    
    void OnServerUpdate(ExHealth health)
    {
        maxHealth = health.maxHealth;
        currentHealth = health.currentHealth;
    }

	// Use this for initialization
	void Start () {
        

	}
	
    void OnDestroy()
    {
        FFMessageBoard<ApplyHealthEvent>.Disconnect(OnApplyHealth, gameObject);
        FFMessageBoard<EmptyHealthEvent>.Disconnect(OnHealthEmpty, gameObject);
        FFMessageBoard<ExHealth>.Disconnect(OnServerUpdate, gameObject);
        FFMessage<FFNetEvents.ClientConnectedEvent>.Disconnect(OnClientConnected);
    }

    // Networked Event
    void OnApplyHealth(ApplyHealthEvent e)
    {
        Debug.Log("ApplyHealthEvent!" +
            "\nApplied via damagegroup:" + e.group +
            "\nDamage done too: " + damageGroup);

        if((e.group & damageGroup).Equals(DamageGroup.None) == false)
        {
            if (e.amount < 0) // damage
            {
                // Might add armor/resistance/modifiers/special/time sensative/etc...
                currentHealth += e.amount;
                if (currentHealth < 0)
                    TriggerHealthEmpty();

            }
            else if (e.amount > 0) // Healing
            {
                // Might add armor/resistance/modifiers/special/time sensative/etc...
                currentHealth += e.amount;
                if (currentHealth < 0)
                    TriggerHealthEmpty();
            }
        }
    }

    void TriggerHealthEmpty()
    {
        EmptyHealthEvent e;
        e.currentHealth = currentHealth;
        FFMessageBoard<EmptyHealthEvent>.SendToLocal(e, gameObject);
        FFMessageBoard<EmptyHealthEvent>.SendToNet(e, gameObject, true);
    }

    // Local Event
    void OnHealthEmpty(EmptyHealthEvent e)
    {
        // All out of health!
        // Start Death Sequence
        TriggerDeathSequence();
    }

    void TriggerDeathSequence()
    {
        deathSequence = action.Sequence();
        // animation,sounds,disable colliders,etc...

        Collider col;
        if (col = GetComponent<Collider>())
            col.enabled = false;

        deathSequence.Sync(); // finish all of sequence before destroying GameObject
        deathSequence.Call(gameObject.Destroy);
    }
}
