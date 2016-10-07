using UnityEngine;
using System.Collections;

public class RiftSpawner : MonoBehaviour 
{
  
  [SerializeField]
  GameObject Rift;
  
  float SpawnTimer = 0.0f;
  
  [SerializeField]
  float SpawnTime = 5.0f;
  
  [SerializeField]
  float SpawnTimeVariance = 2.0f;
  
  GameObject LevelGlobals;
  GameObject CentrePoint;
  GameObject Player;
  GameObject Camera;
  CameraController Camcontrol;
  

  

  // Use this for initialization
  void Start () 
  {
    LevelGlobals = GameObject.FindWithTag("Globals");
    Player = LevelGlobals.GetComponent<LevelGlobals>().Player;
    CentrePoint = LevelGlobals.GetComponent<LevelGlobals>().CentrePoint;
    Camera = LevelGlobals.GetComponent<LevelGlobals>().Camera;
    Camcontrol = Camera.GetComponent<CameraController>();
    SpawnTimer = SpawnTime;

  }
  
  // Update is called once per frame
  void Update () 
  {
    if (Camcontrol.GetPTime() || Camcontrol.GetETime())
      return;
    
    SpawnTimer -= Time.deltaTime;
    if (SpawnTimer <= 0.0)
    {
      LaunchRift();
      SpawnTimeCalc();
    }
  
  }
  
  public void SpawnTimeCalc()
  {
    int stacks = Player.GetComponent<PlayerMovement>().SpeedStacks;
    SpawnTimer = Mathf.Clamp(SpawnTime + Random.Range(-SpawnTimeVariance - stacks/2, SpawnTimeVariance), 0.1f, 10.0f);
  }
  
  public void LaunchRift()
  {
    Vector3 SpawnPos = CentrePoint.transform.forward*350;
    
    float offsetx = (CentrePoint.transform.right.x + CentrePoint.transform.up.x) * 5.0f;
    float offsety = (CentrePoint.transform.right.y + CentrePoint.transform.up.y) * 5.0f;
    float offsetz = (CentrePoint.transform.right.z + CentrePoint.transform.up.z) * 5.0f;
    Vector3 offset = new Vector3(Random.Range(-offsetx, offsetx), Random.Range(-offsety, offsety),
            Random.Range(-offsetz, offsetz));
    
  
    //Have random location be in one of the 9 sections at the very least
    int QuadrantChance = (int)Random.Range(0,100);
    if (QuadrantChance < 25) // upperleft
    {
      SpawnPos += (CentrePoint.transform.position + CentrePoint.transform.Find("1").transform.position)/2;
    }
    else if (QuadrantChance >= 25 && QuadrantChance < 50) // upperright
    {
      SpawnPos += (CentrePoint.transform.position + CentrePoint.transform.Find("3").transform.position)/2;
    }
    else if (QuadrantChance >= 50 && QuadrantChance < 75) // lowerleft
    {
      SpawnPos += (CentrePoint.transform.position + CentrePoint.transform.Find("7").transform.position)/2;
    }
    else // lowerright
    {
      SpawnPos += (CentrePoint.transform.position + CentrePoint.transform.Find("9").transform.position)/2;
    }
    
    SpawnPos += offset;
    
    /*int CreationChance = (int)Mathf.Clamp(Random.Range(0.0f,100.0f + Player.GetComponent<PlayerMovement>().SpeedStacks),
                                          0, 300); //max craziness limiter to prevent only large Rifts from spawning
   
    if (CreationChance < 75)
    {
      GameObject Rift = (GameObject)Instantiate(SmallRift, SpawnPos, Quaternion.identity);
    }
    else if (CreationChance >= 75 && CreationChance < 150)
    {
      GameObject Rift = (GameObject)Instantiate(MediumRift, SpawnPos, Quaternion.identity);
    }
    else
    {
      GameObject Rift = (GameObject)Instantiate(LargeRift, SpawnPos, Quaternion.identity);
    }
    */
    GameObject tRift = (GameObject)Instantiate(Rift, SpawnPos, Quaternion.identity);
  }
}
