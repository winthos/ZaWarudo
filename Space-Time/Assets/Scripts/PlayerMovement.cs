using UnityEngine;
using System.Collections;


  // Controls player's individual movement
public class PlayerMovement : MonoBehaviour 
{

  float MovementSpeed = 11.0f;
  public int SpeedStacks = 0;
  [SerializeField]
  float MaximumDistance = 5.0f;
  
  GameObject LevelGlobals;
  GameObject CentrePoint;
  GameObject Camera;
  CameraController Camcontrol;
  
  

  // Use this for initialization
  void Start () 
  {
    
    LevelGlobals = GameObject.FindWithTag("Globals");
    CentrePoint = LevelGlobals.GetComponent<LevelGlobals>().CentrePoint;
    Camera = LevelGlobals.GetComponent<LevelGlobals>().Camera;
    Camcontrol = Camera.GetComponent<CameraController>();
  }
  
  // Update is called once per frame
  void Update () 
  {
      // if we're in normal time
    if (!Camcontrol.GetPTime() && !Camcontrol.GetETime())
    {
      Vector3 dir = new Vector3();
      //allow WASD input yay
      dir = transform.position;
      
      if (Input.anyKey)
      {
        //Vector3 up = transform.position * transform.up;
        //Vector3 centrup = CentrePoint.transform.position * transform.up;
         //if transform (up) is less than centrepoint's transform (up)
        print(transform.up);
        if (Input.GetKey("w") && transform.position.y <= CentrePoint.transform.position.y + MaximumDistance)
        {
           dir += transform.up * MovementSpeed;
        }
        else if (Input.GetKey("s") && transform.position.y >= CentrePoint.transform.position.y - MaximumDistance)
        {
          dir -= transform.up * MovementSpeed;
        }
        
        if (Input.GetKey("a") && transform.position.x >= CentrePoint.transform.position.x - MaximumDistance)
        {
          dir -= transform.right * MovementSpeed;
        }
        else if (Input.GetKey("d") && transform.position.x <= CentrePoint.transform.position.x + MaximumDistance)
        {
          dir += transform.right * MovementSpeed;
        }
      }
      else
        dir = CentrePoint.transform.position;
      
      
      transform.position = Vector3.MoveTowards(transform.position, dir, Time.deltaTime * MovementSpeed);
      
      
    }
    //if in normal time
      //if keys WASD are held, move toward a specific direction
      //if no keys are held, gravitate back toward the centre
    
  }
  
  void OnCollisionEnter(Collision collision)
  {
      // if colliding with an asteroid (or anything remotely hazardous
    if (collision.gameObject.tag == "Hazard")
    {
      GetComponent<Health>().DecrementHealth();
    }
      // if colliding with a rift (or collectible)
    else if (collision.gameObject.tag == "Rift")
    {
      SpeedStacks++;
    }
    
    
  }
  
  void RecalculateBounds()
  {
    
  }
}
