using UnityEngine;
using System.Collections;

public class LevelGlobals : MonoBehaviour 
{
  public GameObject Player;
  public GameObject CentrePoint;
  public GameObject Camera;

	// Use this for initialization
	void Start () 
  {
    Camera = GameObject.FindWithTag("MainCamera");
    Player = GameObject.FindWithTag("Player");
    CentrePoint = GameObject.FindWithTag("Centrepoint");
	}
	
	// Update is called once per frame
	void Update () 
  {
	
	}
}
