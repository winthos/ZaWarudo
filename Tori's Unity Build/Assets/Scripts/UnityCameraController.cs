using UnityEngine;
using System.Collections;

public class UnityCameraController : MonoBehaviour
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

	// Use this for initialization
	void Start ()
    {
	    
	}
	
	// Update is called once per frame
	void Update ()
    {
	
	}
}
