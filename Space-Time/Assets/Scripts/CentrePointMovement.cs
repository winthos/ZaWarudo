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
  
  float DefaultFieldOfView;
  [SerializeField]
  float SpedUpFieldOfView = 80;
  
  bool SpeedUp = false;
  float SpeedTime;
  
    // Use this for initialization
  void Start () 
  {
    LevelGlobals = GameObject.FindWithTag("Globals");
    Camera = LevelGlobals.GetComponent<LevelGlobals>().Camera;
    Camcontrol = Camera.GetComponent<CameraController>();
    Player = LevelGlobals.GetComponent<LevelGlobals>().Player;
    DefaultFieldOfView = Camera.GetComponent<Camera>().fieldOfView;
  }
    
    // Update is called once per frame
  void Update () 
  {
    if (!Camcontrol.GetPTime() && !Camcontrol.GetETime() && !Camcontrol.IsTimeTransitioning())
    {
      if (Input.GetMouseButton(0) && !SpeedUp)
      {
        SpeedUp = true;
        SpeedTime = Time.time;
        
      }
      else if (Input.GetMouseButtonUp(0) && SpeedUp)
      {
        SpeedUp = false;
        SpeedTime = Time.time;
        print("SpeedDown");
      }
      transform.position += transform.forward * GetTrueSpeed();
      LevelGlobals.GetComponent<LevelGlobals>().Camera.transform.position += transform.forward * GetTrueSpeed();
      UpdateFieldOfView();
    
    }  
    //Moving forward during normal time now and forever
  }
  
  public float GetMovementSpeed()
  {
    if (SpeedUp)
      return MovementSpeed * 2;
    else
      return MovementSpeed;
  }
  
  
  
  float GetTrueSpeed()
  {
    return Mathf.Clamp(GetMovementSpeed() * Player.GetComponent<PlayerMovement>().SpeedStacks * StackGainMultiplier,GetMovementSpeed(),100);
  }
  
  void UpdateFieldOfView()
  {
    if (SpeedUp)
      Camera.GetComponent<Camera>().fieldOfView = Mathf.Lerp(Camera.GetComponent<Camera>().fieldOfView, SpedUpFieldOfView, Time.time - SpeedTime);
    else
      Camera.GetComponent<Camera>().fieldOfView = Mathf.Lerp(Camera.GetComponent<Camera>().fieldOfView, DefaultFieldOfView, Time.time - SpeedTime);
  }
}
