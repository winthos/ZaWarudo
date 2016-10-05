using UnityEngine;
using System.Collections;

public class Health : MonoBehaviour 
{
    // The character's starting HP
  [SerializeField]
  private int hp = 1;
    // Maximum HP of the character
  private int mhp;
 
    // Determines whether or not the character immediately explodes upon reaching 0 HP
  [SerializeField]
  public bool DestroyAtZero;
  
  public int health
  {
    get { return hp; }
    set { hp = value; }
    
  }
    // Use this for initialization
  void Start () 
  {
    if (hp < 0)
      hp = 1;
    mhp = hp;
  }
    
    // Update is called once per frame
  void Update ()
  {
    
  }
  
  public void DecrementHealth()
  {
    hp--;
    //if (hp == 0)
      //destroy!
  }
}
