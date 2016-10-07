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
  
  Vector3 DashDestination;
  GameObject DashTo;
  
  float moveTime = 0.0f;
  
  [SerializeField]
  float MinDashTimeNeeded = 1.0f;

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
    if (!Camcontrol.GetPTime() && !Camcontrol.GetETime() && !Camcontrol.IsTimeTransitioning())
    {
      Vector3 dir = new Vector3();
      //allow WASD input yay
      dir = transform.position;
      
      if (MovementKeyDown())
      {
        //Vector3 up = transform.position * transform.up;
        //Vector3 centrup = CentrePoint.transform.position * transform.up;
         //if transform (up) is less than CentrePoint's transform (up)
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
        else if (Input.GetKey("space")) // if middle mouse
        {
          dir = CentrePoint.transform.position;
        }
      }
      //else
       // dir = CentrePoint.transform.position;
      
      
      transform.position = Vector3.MoveTowards(transform.position, dir, Time.deltaTime * MovementSpeed);
      
      
    }
    else if (Camcontrol.GetPTime() || (Camcontrol.GetPTime() && Camcontrol.GetETime()))
    {
      //key pressed -> store grid location -> quickly lerp over -> new location!
      if (Input.anyKey && DashTo == null && Camcontrol.GetPTimeStopTimer() > MinDashTimeNeeded)
      {
        if (Input.GetKeyDown("w") && Input.GetKeyDown("a")) // 1
        {
          DashDestination = CentrePoint.transform.Find("1").transform.position;
          DashTo = CentrePoint.transform.Find("1").gameObject;
        }
        else if (Input.GetKeyDown("w") && Input.GetKeyDown("d")) //3
        {
          DashDestination = CentrePoint.transform.Find("3").transform.position;
          DashTo = CentrePoint.transform.Find("3").gameObject;
        } 
        else if (Input.GetKeyDown("s") && Input.GetKeyDown("a")) //7
        {
          DashDestination = CentrePoint.transform.Find("7").transform.position;
          DashTo = CentrePoint.transform.Find("7").gameObject;
        } 
        else if (Input.GetKeyDown("s") && Input.GetKeyDown("d")) //9
        {
          DashDestination = CentrePoint.transform.Find("9").transform.position;
          DashTo = CentrePoint.transform.Find("9").gameObject;
        } 
        else if (Input.GetKeyDown("w")) //2
        {
          DashDestination = CentrePoint.transform.Find("2").transform.position;
          DashTo = CentrePoint.transform.Find("2").gameObject;
        }
        else if (Input.GetKeyDown("s")) //8
        {
          DashDestination = CentrePoint.transform.Find("8").transform.position;
          DashTo = CentrePoint.transform.Find("8").gameObject;
        }
        else if (Input.GetKeyDown("a")) //4
        {
          DashDestination = CentrePoint.transform.Find("4").transform.position;
          DashTo = CentrePoint.transform.Find("4").gameObject;
        }
        else if (Input.GetKeyDown("d")) //6
        {
          DashDestination = CentrePoint.transform.Find("6").transform.position;
          DashTo = CentrePoint.transform.Find("6").gameObject;
        }
        else if (Input.GetKeyDown("space")) //5
        {
          DashDestination = CentrePoint.transform.position;
          DashTo = CentrePoint;
        }
        moveTime = Time.time;
      }
      
      if (DashTo != null && DashDestination != DashTo.transform.position)
        DashDestination = DashTo.transform.position;
      
      if (DashTo != null && DashDestination != Vector3.zero && transform.position != DashDestination)
      {
        print(DashDestination);
        transform.position = Vector3.Lerp(transform.position, DashDestination, 0.125f);
        if (Vector3.Distance(transform.position,DashDestination) < 0.1)
        {
          //CentrePoint.transform.position = transform.position;
          //DashDestination = Vector3.zero;
          DashTo = null;
          
        }
      }
      
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
      print("OW");
      GetComponent<Health>().DecrementHealth();
    }
      // if colliding with a rift (or collectible)
    
    
    
  }
  
  void OnTriggerEnter(Collider other)
  {
    if (other.gameObject.tag == "Rift")
    {
      SpeedStacks++;
      Camcontrol.IncreasePStopTime(1.0f);
      Destroy(other.gameObject);
    }
  }
  
  void RecalculateBounds()
  {
    
  }
  
  bool MovementKeyDown()
  {
    return Input.GetKey("w") || Input.GetKey("a") || Input.GetKey("s") || Input.GetKey("d") || Input.GetKey("space");
  }
  
  public void ResetDashDestination()
  {
    DashTo = null;
    //DashDestination = Vector3.zero;
  }
}
