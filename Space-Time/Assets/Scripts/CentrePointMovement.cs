using UnityEngine;
using System.Collections;

  //Specifically for the constant move in the forward direction during normal time
public class CentrePointMovement : MonoBehaviour 
{
  [SerializeField]
  float MovementSpeed = 0.1f;
  GameObject LevelGlobals;
  GameObject Camera;
  CameraController Camcontrol;
    // Use this for initialization
  void Start () 
  {
    LevelGlobals = GameObject.FindWithTag("Globals");
    Camera = LevelGlobals.GetComponent<LevelGlobals>().Camera;
    Camcontrol = Camera.GetComponent<CameraController>();
  }
    
    // Update is called once per frame
  void Update () 
  {
    if (!Camcontrol.GetPTime() && !Camcontrol.GetETime())
    {
      transform.position += transform.forward * MovementSpeed;
       LevelGlobals.GetComponent<LevelGlobals>().Camera.transform.position += transform.forward * MovementSpeed;
    }  
    //Moving forward during normal time now and forever
  }
  
  public float GetMovementSpeed()
  {
    return MovementSpeed;
  }
}
