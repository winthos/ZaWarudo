using UnityEngine;
using System.Collections;

public class TimeStopFX : MonoBehaviour {

    CameraController CameraScript;
    bool timeStopped;
    bool timeFXplayed = false;

	// Use this for initialization
	void Start ()
    {
        CameraScript = GameObject.Find("Main Camera").GetComponent<CameraController>();
        timeStopped = CameraScript.GetPTime();
    }
	
	// Update is called once per frame
	void Update ()
    {
        timeStopped = CameraScript.GetPTime();

        if (!timeStopped)
            timeFXplayed = true;


        if (timeStopped && timeFXplayed)
        {
            AkSoundEngine.PostEvent("timeStopSound", this.gameObject);
            timeFXplayed = false;
        }
	}
}
