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
  GameObject Player;
  [SerializeField]
  float StackGainMultiplier = 1.0f;
    // Use this for initialization
  void Start () 
  {
    LevelGlobals = GameObject.FindWithTag("Globals");
    Camera = LevelGlobals.GetComponent<LevelGlobals>().Camera;
    Camcontrol = Camera.GetComponent<CameraController>();
    Player = LevelGlobals.GetComponent<LevelGlobals>().Player;
  }
    
    // Update is called once per frame
  void Update () 
  {
    if (!Camcontrol.GetPTime() && !Camcontrol.GetETime())
    {
      transform.position += transform.forward * GetTrueSpeed();
      LevelGlobals.GetComponent<LevelGlobals>().Camera.transform.position += transform.forward * GetTrueSpeed();
    }  
    //Moving forward during normal time now and forever
  }
  
  public float GetMovementSpeed()
  {
    return MovementSpeed;
  }
  
  float GetTrueSpeed()
  {
    return Mathf.Clamp(MovementSpeed * Player.GetComponent<PlayerMovement>().SpeedStacks * StackGainMultiplier,MovementSpeed,100);
  }
}
