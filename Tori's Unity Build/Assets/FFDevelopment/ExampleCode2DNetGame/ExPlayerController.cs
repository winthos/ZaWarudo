using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Threading;
using FFNetEvents;
using FFLocalEvents;


public class ExPlayerController : FFComponent {
    // Sequences
    FFAction.ActionSequence MoveSeq;
    FFAction.ActionSequence PositionUpdateSeq;

    // Resources
    public GameObject Bullet;
    public Vector3 FiringLocation = new Vector3(0,100,0);

    // Events
    [Serializable]
    public struct ExPlayerMoveAction
    {
        public double time;
        public FFVector3 position;
        public FFVector3 moveDirection;
    }

    [Serializable]
    public struct ExPlayerFireAction
    {
        public double time;
        public FFVector3 position;
    }

    [Serializable]
    public struct ExPlayerPositionUpdate
    {
        public double time;
        public FFVector3 position;
    }

    // Player Data
    public float PlayerSpeed = 900.0f;
    private Vector3 _moveVec;

    void Awake()
    {
        bool localNetObject = FFSystem.RegisterNetGameObject(gameObject, "Player");

        // Make Sequences
        MoveSeq = action.Sequence();
        // Update Sequence

        // Input Events only if object was created by local source
        if (localNetObject)
        {
            FFMessage<FFLocalEvents.UpdateEvent>.Connect(OnUpdateEvent);

            PositionUpdateSeq = action.Sequence();
            PositionUpdateSeq.Call(UpdatePositionCall);
        }

        FFMessageBoard<ExPlayerPositionUpdate>.Connect(OnPlayerPositionUpdate, gameObject);
        FFMessageBoard<ExPlayerMoveAction>.Connect(OnPlayerMoveAction, gameObject);
        FFMessageBoard<ExPlayerFireAction>.Connect(OnPlayerFireAction, gameObject);
    }

	void Start ()
    {

	}
    
    void OnDestroy()
    {
        // Events
        FFMessageBoard<ExPlayerMoveAction>.Disconnect(OnPlayerMoveAction, gameObject);
        FFMessageBoard<ExPlayerFireAction>.Disconnect(OnPlayerFireAction, gameObject);
        FFMessageBoard<ExPlayerPositionUpdate>.Disconnect(OnPlayerPositionUpdate, gameObject);
        FFMessage<FFLocalEvents.UpdateEvent>.Disconnect(OnUpdateEvent);
    }

	// Update is called once per frame
    void OnUpdateEvent(FFLocalEvents.UpdateEvent e)
    {
        FFVector3 moveDirection;
        moveDirection.x = Input.GetAxis("Horizontal");
        moveDirection.y = Input.GetAxis("Vertical");
        moveDirection.z = 0;

        if (Vector3.Magnitude(moveDirection) >= 1)
        {
            moveDirection = Vector3.Normalize(moveDirection);
        }

        // optimization: Don't send if its not different from the last
        // move packet sent
        if(_moveVec != moveDirection)
        {
            ExPlayerMoveAction moveEvent;
            moveEvent.moveDirection = moveDirection;
            moveEvent.time = FFSystem.time;
            moveEvent.position = gameObject.transform.position;

            FFMessageBoard<ExPlayerMoveAction>.SendToLocal(moveEvent, gameObject);
            FFMessageBoard<ExPlayerMoveAction>.SendToNet(moveEvent, gameObject, false);
            _moveVec = moveDirection;
            //Debug.Log("Moving!"); // debug
        }

        

        if(Input.GetButtonDown("Fire1"))
        {
            ExPlayerFireAction ePFA;
            ePFA.position = transform.position + FiringLocation;
            ePFA.time = FFSystem.time;

            FFMessageBoard<ExPlayerFireAction>.SendToLocal(ePFA, gameObject);
            FFMessageBoard<ExPlayerFireAction>.SendToNet(ePFA, gameObject, true);
            //Debug.Log("Fire1"); //debug
        }

	}
    private void OnPlayerPositionUpdate(ExPlayerPositionUpdate e)
    {
        // TODO maybe add a time validation so that we don't get packets from a long time ago
        transform.position = e.position;
    }
    private void OnPlayerMoveAction(ExPlayerMoveAction e)
    {
        // Set position
        transform.position = e.position;
        Vector3 moveDirection = e.moveDirection;

        // clear sequence and add new one, time warp to catup on
        // our game
        MoveSeq.ClearSequence();
        MoveSequenceCall(moveDirection);
        MoveSeq.TimeWarpFrom(e.time);
    }

    private void OnPlayerFireAction(ExPlayerFireAction e)
    {
        var newBullet = (GameObject)GameObject.Instantiate(Bullet, e.position, new Quaternion());
        ExBullet bulletScript = newBullet.GetOrAddComponent<ExBullet>();

        bulletScript.FireBullet(e.time, e.position);
    }

    private void UpdatePositionCall()
    {
        ExPlayerPositionUpdate e;
        e.position = transform.position;
        e.time = FFSystem.time;
        FFMessageBoard<ExPlayerPositionUpdate>.SendToNet(e, gameObject, false);

        PositionUpdateSeq.Delay(0.6f);
        PositionUpdateSeq.Sync();
        PositionUpdateSeq.Call(UpdatePositionCall);
    }
    private void MoveSequenceCall(object moveDirection)
    {
        Vector3 moveDir = (Vector3)moveDirection;
        const float callTime = 3.0f;

        MoveSeq.Property(ffposition, ffposition + (moveDir * PlayerSpeed * callTime), FFEase.E_Continuous, callTime);
        MoveSeq.Sync();
        MoveSeq.Call(MoveSequenceCall, moveDir);
    }
}
