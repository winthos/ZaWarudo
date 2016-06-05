using UnityEngine;
using System.Collections;

public class NewBehaviourScript : MonoBehaviour
{
    //need a camera so we can see stuff 
    Camera cameraObj = null;
    
    //grab the things we need as Cogs because I'm putting
    //everything on this one script instead of seperating it into
    //components like I should because
    //IM AN ADULT AND CAN DO WHAT I WANT JORDAN 
    GameObject Player = null;
    GameObject Mount = null;
    GameObject WorldFilter = null;
    GameObject RotationPivot = null;
    GameObject TimerText = null;
    GameObject EnemyWorldFilter = null;
    GameObject EnemyTimerText = null;
    
    //this changes forward movement speed while in normal time
    float MovementSpeed = 0.04f;
    
    // Used to store mouse state so we can use it during logic update.
    bool LeftMouseDown = false;
    bool RightMouseDown = false;
    //this is the change in mouse position stored as a 2D vector
    Vector2 MouseDelta = new Vector2(0.0f, 0.0f);

    //This is true only when in normal time, since, in normal time Soshite toki ga ugoki desu....
    //I also use this to basically check if we are in stopped time or not
    bool Soshitetokigaugokidesu = true;

    //used to indicate if an Enemy has stopped time, but the player may not have stopped time yet.
    bool OnagiTypeNoSutando = false;
    
    //used to indicate that I changed the Thing already and don't need to do it again
    bool Ichangedthethingalready = true;

    //stores world forward for the player if we need it I guess
    Vector3 Worldforward = new Vector3();

    //counts how long we have spent in stopped time (seconds)
    float Ichibyoukeika = 0.0f;
    
    //counts how long the enemy has spent in stopped time
    float OreOTokiOTometa = 0.0f;

	// Use this for initialization
	void Start ()
    {
        //Setting up GameObject reference paths (Equivalent of Cogs in Zero)
        Player = GameObject.Find("Player");
        Mount = GameObject.Find("Mount");
        WorldFilter = GameObject.Find("WorldFilter");
        EnemyWorldFilter = GameObject.Find("EnemyWorldFilter");
        RotationPivot = GameObject.Find("RotationPivot");
        TimerText = GameObject.Find("Timer");
        EnemyTimerText = GameObject.Find("EnemyTimer");
    }
	
	// Update is called once per frame
	void Update ()
    {
	
	}

    ////////////////////////////////////

    //MOUSE DOWN TIME IS STOPPED
    //LEFT MOUSE STUFF IS FOR ROTATING WHILE IN STOPPED TIME

    ////////////////////////////////////
    void OnMouseDown()
    {
        //Left Mouse Logic
        if (Input.GetMouseButtonDown(0))
        {
            LeftMouseDown = true;
            Debug.Log("Left mouse is donut");
        }

        //Right Mouse Logic
        if (Input.GetMouseButtonDown(1))
        {
            RightMouseDown = true;
            Debug.Log("right mouse is just done right now");

            if (Soshitetokigaugokidesu == true)
            {
                Soshitetokigaugokidesu = false;
                WorldFilter.GetComponent<MeshRenderer>().enabled = true;
                //var hinjaku = new DioShout();
                //GameSession.DispatchEvent("THE WORLD", hinjaku);

                //Creating a circle? I dunno
                Instantiate(Resources.Load("ZaWarudoCircle"), Player.transform.position, Quaternion.identity);
            }

            else if (Soshitetokigaugokidesu == false)
            {
                Soshitetokigaugokidesu = true;
                WorldFilter.GetComponent<MeshRenderer>().enabled = false;
            }
        }
    }

    void OnMouseUp()
    {
        if (Input.GetMouseButtonUp(0))
        {
            LeftMouseDown = false;
            Debug.Log("LeftMouse is turnt uppppp");
        }

        if (Input.GetMouseButtonUp(1))
        {
            RightMouseDown = false;
            Debug.Log("RightMouse is rightmouse");
        }
    }

    void OnMouseMove()
    {
        MouseDelta =  MouseDelta - new Vector2(Input.mousePosition.x, Input.mousePosition.y);
    }
}
