using UnityEngine;
using System.Collections;

public class AsteroidSpawner : MonoBehaviour 
{
  
  [SerializeField]
  GameObject SmallAsteroid;
  [SerializeField]
  GameObject MediumAsteroid;
  [SerializeField]
  GameObject LargeAsteroid;
  
  float SpawnTimer = 0.0f;
  
  [SerializeField]
  float SpawnTime = 5.0f;
  
  [SerializeField]
  float SpawnTimeVariance = 2.0f;
  
  float Spawns = 0;
  
  [SerializeField]
  int BigSpawn = 6;

  [SerializeField]
  int CreationChanceModifier = 50;
  
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
    /*
    Instantiate(enemyPrefab, newPos, Quaternion.identity
    */
  }
  
  // Update is called once per frame
  void Update () 
  {
    if (Camcontrol.GetPTime() || Camcontrol.GetETime())
      return;
    
    SpawnTimer -= Time.deltaTime;
    if (SpawnTimer <= 0.0)
    {
      LaunchAsteroid(0);
      SpawnTimeCalc();
    }

    if(Input.GetKeyDown("j"))
        {
            LaunchAsteroid(3);
        }
  
  }
  
  public void SpawnTimeCalc()
  {
    int stacks = Player.GetComponent<PlayerMovement>().SpeedStacks;
    SpawnTimer = Mathf.Clamp(SpawnTime + Random.Range(-SpawnTimeVariance - stacks/2, SpawnTimeVariance), 0.1f, 10.0f);
  }
  
  public void LaunchAsteroid(int spawntype) // 0 = normal, 1 = small, 2 = medium, 3 = large
  {
    Vector3 SpawnPos = CentrePoint.transform.position + CentrePoint.transform.forward*500;
    
    float offsetx = (CentrePoint.transform.right.x + CentrePoint.transform.up.x) * 5.0f;
    float offsety = (CentrePoint.transform.right.y + CentrePoint.transform.up.y) * 5.0f;
    float offsetz = (CentrePoint.transform.right.z + CentrePoint.transform.up.z) * 5.0f;
    Vector3 offset = new Vector3(Random.Range(-offsetx, offsetx), Random.Range(-offsety, offsety),
            Random.Range(-offsetz, offsetz));
    SpawnPos += offset;
  
    //Have random location be in one of the 9 sections at the very least
  
    
    int CreationChance = (int)Mathf.Clamp(Random.Range(0.0f,100.0f + Player.GetComponent<PlayerMovement>().SpeedStacks),
                                          0, 300); //max craziness limiter to prevent only large asteroids from spawning
    if (Spawns % BigSpawn == 0)
    {
      CreationChance += CreationChanceModifier;
    }
        if (spawntype == 1)
            CreationChance = 0;
        else if (spawntype == 2)
            CreationChance = 76;
        else if (spawntype == 3)
            CreationChance = 151;
    
    /*
      GameObject Asteroid = Instantiate([prefab], [position], Quaternion.identity);
    */
    if (CreationChance < 75)
    {
      GameObject Asteroid = (GameObject)Instantiate(SmallAsteroid, SpawnPos, Quaternion.identity);
    }
    else if (CreationChance >= 75 && CreationChance < 150)
    {
      GameObject Asteroid = (GameObject)Instantiate(MediumAsteroid, SpawnPos, Quaternion.identity);
    }
    else
    {
      GameObject Asteroid = (GameObject)Instantiate(LargeAsteroid, SpawnPos, Quaternion.identity);
    }
    
  }
}
