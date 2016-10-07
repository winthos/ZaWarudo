using UnityEngine;
using System.Collections;

public class LevelGlobals : MonoBehaviour 
{
  public GameObject Player;
  public GameObject CentrePoint;
  public GameObject Camera;
  
  [SerializeField]
  public bool Debugging = true;

	// Use this for initialization
	void Start () 
  {
    Camera = GameObject.FindWithTag("MainCamera");
    Player = GameObject.FindWithTag("Player");
    CentrePoint = GameObject.FindWithTag("Centrepoint");
    Cursor.lockState = CursorLockMode.Locked;
	}
	
	// Update is called once per frame
	void Update () 
  {
    if (Input.GetKey("i"))
      Debugging = !Debugging;
    if (Input.GetKey("escape"))
            Application.Quit();
        
	}
}
